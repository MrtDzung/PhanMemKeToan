namespace PhanMemKeToan.Domain.Enums;

public enum InventoryItemType
{
    /// <summary>Nguyên vật liệu — Raw material. Uses stock accounts (152x). Has stock movements.</summary>
    RawMaterial = 0,
    /// <summary>Thành phẩm — Finished product. Uses stock accounts (155x). Has stock movements.</summary>
    FinishedProduct = 1,
    /// <summary>Hàng hóa — Goods for resale. Uses stock accounts (156x). Has stock movements.</summary>
    Goods = 2,
    /// <summary>Dịch vụ — Service. No stock movements. OpeningBalance rows NOT allowed (BR-IN05).</summary>
    Service = 3
}
