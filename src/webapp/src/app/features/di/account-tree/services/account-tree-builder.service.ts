import { Injectable } from '@angular/core';
import { TreeNode } from 'primeng/api';
import { AccountTreeNodeDto } from '../../models/account.models';

@Injectable({ providedIn: 'root' })
export class AccountTreeBuilderService {
  buildTree(accounts: AccountTreeNodeDto[], expandedNodeIds?: Set<string>): TreeNode[] {
    const sorted = [...accounts].sort((a, b) =>
      a.accountNumber.localeCompare(b.accountNumber)
    );

    const map = new Map<string, TreeNode>();
    const roots: TreeNode[] = [];

    for (const acc of sorted) {
      const node: TreeNode = {
        key: acc.accountId,
        label: acc.accountName,
        data: acc,
        children: [],
        leaf: !acc.isParent,
        selectable: !acc.isParent,
        expanded: expandedNodeIds ? expandedNodeIds.has(acc.accountId) : false,
      };
      map.set(acc.accountId, node);
    }

    for (const acc of sorted) {
      const node = map.get(acc.accountId)!;
      if (acc.parentId && map.has(acc.parentId)) {
        const parent = map.get(acc.parentId)!;
        parent.children = parent.children ?? [];
        parent.children.push(node);
      } else {
        roots.push(node);
      }
    }

    return roots;
  }

  filterTree(accounts: AccountTreeNodeDto[], query: string): AccountTreeNodeDto[] {
    if (!query.trim()) return accounts;

    const lower = query.toLowerCase();
    const matchingIds = new Set<string>();

    for (const acc of accounts) {
      if (
        acc.accountNumber.toLowerCase().includes(lower) ||
        acc.accountName.toLowerCase().includes(lower)
      ) {
        matchingIds.add(acc.accountId);
        // include ancestors
        let parentId = acc.parentId;
        while (parentId) {
          matchingIds.add(parentId);
          const parent = accounts.find((a) => a.accountId === parentId);
          parentId = parent?.parentId ?? null;
        }
      }
    }

    return accounts.filter((a) => matchingIds.has(a.accountId));
  }

  highlightMatch(text: string, query: string): string {
    if (!query.trim()) return text;
    const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return text.replace(new RegExp(`(${escaped})`, 'gi'), '<mark>$1</mark>');
  }
}
