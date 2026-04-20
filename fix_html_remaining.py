import sys
sys.stdout.reconfigure(encoding='utf-8')

f = r"F:\DuAn\PhanMemKeToan\src\webapp\src\app\features\di\inventory-items\components\inventory-item-form\inventory-item-form.component.html"

with open(f, 'r', encoding='utf-8') as fp:
    c = fp.read()

R = '\ufffd'

# Fix remaining 4 garbled chars
c = c.replace(f'Chua c{R} mã vạch', 'Chưa có mã vạch')
c = c.replace(f'Chua c{R} thuộc tính', 'Chưa có thuộc tính')
c = c.replace(f'T{R}nh nang', 'Tính năng')
c = c.replace(f'phi{R}n b?n', 'phiên bản')
c = c.replace(f's? du?c b? sung', 'sẽ được bổ sung')
c = c.replace(f'chi ti?t', 'chi tiết')

with open(f, 'w', encoding='utf-8') as fp:
    fp.write(c)

remaining = c.count('\ufffd')
print(f"Remaining garbled chars: {remaining}")
if remaining > 0:
    lines = c.split('\n')
    for i, line in enumerate(lines):
        if '\ufffd' in line:
            print(f"  Line {i+1}: {line[:150]}")
else:
    print("All garbled chars fixed!")
    # Verify key sections
    idx = c.find('form-toolbar')
    print("\nToolbar:")
    print(c[idx:idx+300])
