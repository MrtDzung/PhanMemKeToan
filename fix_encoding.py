import os

def fix_file(path, replacements):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    for old, new in replacements:
        content = content.replace(old, new)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed: {os.path.basename(path)}")

BASE = r"F:\DuAn\PhanMemKeToan\src\webapp\src\app\features\di"

# Fix 1: inventory-item-form.component.ts — operatorOptions and any other garbled
ts_file = BASE + r"\inventory-items\components\inventory-item-form\inventory-item-form.component.ts"
fix_file(ts_file, [
    ("{ label: '\ufffd', value: '*' }", "{ label: '\u00d7', value: '*' }"),
    ("{ label: '\ufffd', value: '/' }", "{ label: '\u00f7', value: '/' }"),
    # Fix error messages that might be garbled
    ("'L\ufffdng h\ufffd'", "'L\u01b0u h\u1ed3 s\u01a1'"),
    ("'Kh\ufffdng th\ufffd t\ufffdng'", "'Kh\u00f4ng th\u1ec3 t\u1ea3i'"),
])

# Verify fix 1
with open(ts_file, 'r', encoding='utf-8') as f:
    content = f.read()
idx = content.find("operatorOptions")
print("operatorOptions block:")
print(content[idx:idx+120])

# Fix 2: account-objects-list.component.html — already fixed, verify
html_file = BASE + r"\account-objects\components\account-objects-list\account-objects-list.component.html"
with open(html_file, 'r', encoding='utf-8') as f:
    content = f.read()
print("\naccount-objects-list.component.html tabs:")
idx = content.find("status-tabs")
print(content[idx:idx+250])

# Fix 3: inventory-item-form.component.html — check and fix
html2 = BASE + r"\inventory-items\components\inventory-item-form\inventory-item-form.component.html"
with open(html2, 'r', encoding='utf-8') as f:
    content = f.read()
# Find garbled text
garbled_count = content.count('\ufffd')
print(f"\ninventory-item-form.component.html: {garbled_count} garbled chars")
# Show first 500 chars
print(content[:500])
