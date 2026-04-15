# E-commerce Integration Architecture
> Accounting Webapp ↔ E-commerce Storefront — API Integration Pattern

**Referenced by**: constitution.md § Architecture Decisions  
**Status**: Approved  
**Last Updated**: 2026-04-15

## Overview

The accounting webapp exposes REST APIs that an external e-commerce storefront consumes. Separate databases, separate deployments, connected via API + Message Queue + Redis cache. Accounting DB is the **Single Source of Truth** for all financial and inventory data.

```
┌─────────┐     Events/      ┌─────────────┐      REST        ┌───────────┐
│Accounting│────Webhooks─────►│ Message Queue│◄────API────────►│ E-commerce│
│   API    │◄───REST──────────│ (RabbitMQ)  │                  │    API    │
└────┬─────┘                  └──────┬──────┘                  └─────┬─────┘
     │                               │                               │
┌────┴─────┐                  ┌──────┴──────┐                  ┌─────┴─────┐
│SQL Server│                  │   Redis     │                  │ E-com DB  │
│Accounting│                  │ Stock Cache │                  │(Postgres) │
│   DB     │                  │ Price Cache │                  │Cart,Media │
└──────────┘                  └─────────────┘                  └───────────┘
```

## Design Principles

1. **Accounting = Source of Truth**: Stock, pricing, master data live in accounting DB
2. **Separate Databases**: E-com DB owns cart, sessions, media, SEO; never touches accounting tables directly
3. **Event-Driven Sync**: Accounting publishes domain events; e-com subscribes
4. **Idempotent APIs**: Every write request requires `X-Idempotency-Key` header
5. **Graceful Degradation**: If accounting API is down, e-com falls back to cached data + "order pending confirmation" mode

---

## 1. Inventory Sync (CRITICAL)

### Stock Formula

```
QuantityAvailable = QuantityOnHand - QuantityReserved - QuantityEcomHold
```

| Field | Meaning | Updated By |
|---|---|---|
| `QuantityOnHand` | Physical stock after posting IN vouchers | Accounting posting engine |
| `QuantityReserved` | Reserved for internal sales orders (not yet shipped) | Accounting SA module |
| `QuantityEcomHold` | Temporary hold for e-com carts (TTL 15min) | E-com → Accounting API |
| `QuantityAvailable` | Computed, exposed to e-com via Redis cache | Auto-computed |

### Order Lifecycle & Stock

```
Step 1: Add to cart
  → POST /api/inventory/hold {sku, qty, holdId, ttl: 15min}
  → QuantityEcomHold += qty
  → TTL expiry → auto-release hold

Step 2: Checkout (payment confirmed)
  → POST /api/orders/from-ecom {orderLines, customerId, paymentRef}
  → QuantityEcomHold -= qty
  → QuantityReserved += qty
  → Creates SA Voucher (status=Pending)

Step 3: Approve & ship (accountant posts voucher)
  → QuantityReserved -= qty
  → QuantityOnHand -= qty (via IN Outward posting)
  → Event "stock.updated" → Redis cache refresh
```

### Anti-Oversell Mechanisms

- **Pessimistic lock**: `SELECT ... WITH (UPDLOCK)` on InventoryItem during hold/reserve
- **Safety stock buffer**: Config `SafetyStockQty` per SKU — e-com sees `Available - SafetyStock`
- **Circuit breaker**: If Accounting API down → e-com switches to "pre-order, confirm later" mode
- **Daily reconciliation**: Cron compares e-com cached stock vs accounting actual → alert on mismatch

---

## 2. Order Cancellation / Exchange / Return

### Cancellation

| Order State | Accounting Action | E-com Action |
|---|---|---|
| Cart (Hold) | Release hold: `EcomHold -= qty` | Show stock restored |
| Confirmed, not shipped (Reserved) | Cancel SA Voucher, `Reserved -= qty` | Status=Cancelled, refund |
| Shipped (Posted) | Create SA Return Voucher + IN Inward Voucher, reverse posting entries, `OnHand += qty` | Status=Cancelled, refund |

### Exchange

```
POST /api/orders/{id}/exchange
{
  returnLines: [{sku: "A", qty: 1}],
  newLines: [{sku: "B", qty: 1}],
  priceDifference: 50000
}

Accounting (single transaction):
  1. SA Return Voucher for returnLines → stock A back in
  2. New SA Voucher for newLines → hold/reserve stock B
  3. Price difference posting entry
  4. Link OriginalVoucherId → ExchangeVoucherId
```

### Return

```
POST /api/orders/{id}/return
{
  returnLines: [{sku, qty, reason, condition}],
  refundMethod: "original_payment" | "store_credit"
}

Accounting:
  1. SA Return Voucher: Nợ 531 / Có 131
  2. IN Inward Voucher: Nợ 156 / Có 632
  3. QuantityOnHand += qty (after posting)
  4. If condition=damaged → separate warehouse or expense write-off
```

---

## 3. Master Data Sync

**Direction**: Accounting is master for all master data (except e-com-specific fields like SEO, images).

| Data | Master | Direction | Mechanism |
|---|---|---|---|
| Products (name, code, unit, price) | Accounting | Accounting → E-com | Event `product.updated` + Delta API |
| Product media (images, descriptions) | E-com | E-com only | E-com manages independently |
| Customers (B2B) | Accounting | Accounting → E-com | Event `accountobject.updated` |
| Customers (B2C, new signups) | E-com | E-com → Accounting | `POST /api/account-objects` |
| Vendors/Suppliers | Accounting | Accounting → E-com (read-only) | Event push |
| Product categories | Accounting | Accounting → E-com | Event push, e-com maps to storefront categories |
| Units of measure | Accounting | Accounting → E-com | One-time sync, rarely changes |

### Customer Merge Logic

```
New e-com signup with phone/email:
  1. GET /api/account-objects?phone=xxx&email=yyy
  2. If match found → link ecom_customer_id ↔ accountObjectId (no duplicate)
  3. If no match → POST /api/account-objects {type: "Customer", source: "ecommerce"}
```

### Delta Sync API

```
GET /api/catalog/changes?since=2026-04-15T00:00:00Z&limit=100
Response: {
  changes: [{entity, id, action: "created|updated|deleted", data, timestamp}],
  nextCursor: "..."
}
```

E-com polls this every 5 minutes as backup to event-driven sync.

---

## 4. Price & Product Updates

### Price Change Flow

```
Accounting updates UnitPrice
  → Event: "product.price_changed" {sku, oldPrice, newPrice, effectiveDate, priceListId}
  → E-com receives:
     - effectiveDate = now → update listing immediately
     - effectiveDate = future → schedule update
     - priceListId = "B2C" → update storefront
     - priceListId = "B2B" → update B2B portal only
```

### Price Snapshot Rule (NON-NEGOTIABLE)

- Items in cart: price does NOT change mid-session
- New orders: use current price at checkout time
- Confirmed orders: price locked at confirmation time (snapshot stored in VoucherDetail.UnitPrice)
- E-com sends `unitPriceAtOrder` → Accounting validates within ±5% of current price (anti-tampering)

### Product Deactivation

```
Event: "product.deactivated" {sku, reason}
E-com: Hide from storefront listing, show "Hết hàng" on direct URL, reject new cart additions
```

---

## 5. Infrastructure & Reliability

### Message Queue Pattern

```
Accounting publishes events → RabbitMQ Exchange (topic)
  → Queue: ecom.stock.updates (stock changes)
  → Queue: ecom.master.updates (product/customer changes)
  → Queue: ecom.order.status (voucher status changes)

If Accounting down:
  - Messages from e-com queue up, retry when back
  - E-com operates on cached data
  - Dead Letter Queue for messages failing > 3 retries → alert

If E-com down:
  - Accounting events queue up in RabbitMQ (TTL 24h)
  - No impact on accounting operations
```

### Idempotency

Every e-com write request must include:
```
Header: X-Idempotency-Key: ecom-order-{ecomOrderId}
```
Accounting stores processed keys (TTL 72h). Duplicate key → return cached response, no duplicate voucher.

### Daily Reconciliation (Cron)

| Check | Source A | Source B | Action on Mismatch |
|---|---|---|---|
| Stock levels | E-com cached stock | Accounting QuantityAvailable | Auto-correct e-com cache + alert |
| Order status | E-com order status | Accounting voucher status | Sync status + alert |
| Revenue | E-com payment totals | Accounting SA posted totals | Generate diff report for review |

### Order Numbering

```
E-com orders: EC-{YYYYMMDD}-{seq}    (e.g., EC-20260415-0001)
Accounting:   BH-{YYYYMMDD}-{seq}    (e.g., BH-20260415-0001)

Mapping table: EcomOrderId ↔ AccountingVoucherId ↔ RefNo
Always traceable: e-com order → accounting voucher
```

### Multi-Warehouse Support

```
GET /api/inventory/{sku}/warehouses
→ [{warehouseId, warehouseName, quantityAvailable, city}]

E-com uses this to:
  - Show "Còn hàng tại kho gần bạn"
  - Select optimal warehouse for shipping
  - Display per-warehouse stock on product page
```

### Security

| Concern | Solution |
|---|---|
| Authentication | API Key + JWT (service-to-service) |
| Rate limiting | 100 req/s read, 20 req/s write |
| Network | IP whitelist (e-com server only) |
| Webhook verification | HMAC-SHA256 signature |
| Transport | HTTPS / TLS 1.3 only |
| Price tampering | Server-side price validation ±5% |

### Monitoring & Alerts

| Metric | Target | Alert Threshold |
|---|---|---|
| Sync lag (stock change → e-com cache) | < 3s | > 10s |
| Message queue depth | < 100 | > 1000 |
| Failed API calls (retry exhausted) | 0 | > 3 in 5 min |
| Stock mismatch (daily reconciliation) | 0 SKUs | Any mismatch |
| Order sync rate (e-com → voucher) | 100% | < 99% |

---

## API Endpoints Summary (Accounting exposes)

### Inventory
- `POST /api/inventory/hold` — Hold stock for e-com cart
- `DELETE /api/inventory/hold/{holdId}` — Release hold
- `GET /api/inventory/{sku}` — Current available stock
- `GET /api/inventory/{sku}/warehouses` — Stock per warehouse

### Orders
- `POST /api/orders/from-ecom` — Create SA Voucher from e-com order
- `POST /api/orders/{id}/cancel` — Cancel order
- `POST /api/orders/{id}/exchange` — Exchange items
- `POST /api/orders/{id}/return` — Return items
- `GET /api/orders/{id}/status` — Check voucher/posting status

### Master Data
- `GET /api/catalog/changes?since=` — Delta sync for products
- `GET /api/account-objects?phone=&email=` — Customer lookup
- `POST /api/account-objects` — Create customer from e-com
- `GET /api/products/{sku}` — Product detail with current price

### Webhooks (Accounting → E-com)
- `product.created`, `product.updated`, `product.deactivated`
- `product.price_changed`
- `stock.updated`
- `order.status_changed` (pending → approved → shipped → completed)
- `accountobject.updated`
