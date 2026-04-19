import sys
sys.stdout.reconfigure(encoding='utf-8')

f = r"F:\DuAn\PhanMemKeToan\src\webapp\src\app\features\di\inventory-items\components\inventory-item-form\inventory-item-form.component.html"

with open(f, 'r', encoding='utf-8') as fp:
    c = fp.read()

R = '\ufffd'  # replacement char placeholder

# Comprehensive mapping of garbled → correct Vietnamese
replacements = [
    # Toolbar
    (f'Luu & Th{R}m (Ctrl+Shift+S)', 'Lưu & Thêm (Ctrl+Shift+S)'),
    (f'label="{R}{R}ng"', 'label="Đóng"'),
    # Tabs
    (f'>Th{R}ng tin chung<', '>Thông tin chung<'),
    (f'>{R}on v? t{R}nh ph?<', '>Đơn vị tính phụ<'),
    (f'>M{R} v?ch<', '>Mã vạch<'),
    (f'>Thu?c t{R}nh<', '>Thuộc tính<'),
    (f'>{R}?nh m?c NVL<', '>Định mức NVL<'),
    (f'>C{R}i d?t kho<', '>Cài đặt kho<'),
    # Comments
    (f'<!-- Tab 0: Th{R}ng tin chung -->', '<!-- Tab 0: Thông tin chung -->'),
    (f'<!-- Tab 1: {R}on v? t{R}nh ph? -->', '<!-- Tab 1: Đơn vị tính phụ -->'),
    (f'<!-- Tab 2: M{R} v?ch -->', '<!-- Tab 2: Mã vạch -->'),
    (f'<!-- Tab 3: Thu?c t{R}nh -->', '<!-- Tab 3: Thuộc tính -->'),
    (f'<!-- Tab 4: {R}?nh m?c NVL -->', '<!-- Tab 4: Định mức NVL -->'),
    (f'<!-- Tab 5: C{R}i d?t kho -->', '<!-- Tab 5: Cài đặt kho -->'),
    # Field labels (Tab 0)
    (f'<label>M{R} VTHH<', '<label>Mã VTHH<'),
    (f'>M{R} kh{R}ng h?p l?<', '>Mã không hợp lệ<'),
    (f'<label>T{R}n VTHH<', '<label>Tên VTHH<'),
    (f'>T{R}n kh{R}ng h?p l?<', '>Tên không hợp lệ<'),
    (f'<label>T{R}n ti?ng Anh<', '<label>Tên tiếng Anh<'),
    (f'<label>Nh{R}m VTHH<', '<label>Nhóm VTHH<'),
    (f'<label>T{R}nh ch?t<', '<label>Tính chất<'),
    (f'<label>Phuong ph{R}p t{R}nh gi{R}<', '<label>Phương pháp tính giá<'),
    (f'<label>{R}VT ch{R}nh<', '<label>ĐVT chính<'),
    (f'<label>{R}VT bao b{R}/Th{R}ng<', '<label>ĐVT bao bì/Thùng<'),
    (f'<label>{R}on gi{R} b{R}n 1<', '<label>Đơn giá bán 1<'),
    (f'<label>{R}on gi{R} b{R}n 2<', '<label>Đơn giá bán 2<'),
    (f'<label>Kh{R}c<', '<label>Khác<'),
    # Tab 1
    (f'<!-- Tab 1: ', '<!-- Tab 1: '),  # already handled above
    (f'label="Th{R}m {R}VT ph?"', 'label="Thêm ĐVT phụ"'),
    (f'>{R}VT ph?<', '>ĐVT phụ<'),
    (f'>Ph{R}p t{R}nh<', '>Phép tính<'),
    (f'>Ghi ch{R}<', '>Ghi chú<'),
    (f'>Chua c{R} don v?', '>Chưa có đơn vị'),
    (f't{R}nh ph?<', 'tính phụ<'),
    # Tab 2
    (f'label="Th{R}m m{R} v?ch"', 'label="Thêm mã vạch"'),
    (f'>Chua c{R} m{R} v?ch<', '>Chưa có mã vạch<'),
    # Tab 3
    (f'label="Th{R}m thu?c t{R}nh"', 'label="Thêm thuộc tính"'),
    (f'>Lo?i thu?c t{R}nh<', '>Loại thuộc tính<'),
    (f'>Gi{R} tr?<', '>Giá trị<'),
    (f'>Chua c{R} thu?c t{R}nh<', '>Chưa có thuộc tính<'),
    # Tab 4
    (f'>{R}?nh m?c NVL<', '>Định mức NVL<'),
    (f'pTooltip="{R}i t?i trang {R}?nh m?c"', 'pTooltip="Đi tới trang Định mức"'),
    (f'T{R}nh n{R}ng chi ti?t d?nh m?c NVL s? du?c b? sung trong phi{R}n b?n sau.',
     'Tính năng chi tiết định mức NVL sẽ được bổ sung trong phiên bản sau.'),
    # Tab 5
    (f'>C{R}i d?t kho<', '>Cài đặt kho<'),
    (f'<label>Th?i gian d?t h{R}ng (ng{R}y)<', '<label>Thời gian đặt hàng (ngày)<'),
    (f'>Theo d{R}i theo s? Serial<', '>Theo dõi theo số Serial<'),
    (f'>Theo d{R}i theo l{R} h{R}ng<', '>Theo dõi theo lô hàng<'),
    (f'>Theo d{R}i h?n s? d?ng<', '>Theo dõi hạn sử dụng<'),
    (f'>Khai b{R}o h{R}ng theo th{R}ng<', '>Khai báo hàng theo thùng<'),
    # remaining ?  patterns  
    (f'v? t{R}nh ph?', 'vị tính phụ'),
    (f'm{R} v?ch', 'mã vạch'),
    (f'thu?c t{R}nh', 'thuộc tính'),
    (f'd?nh m?c', 'định mức'),
]

count = 0
for old, new in replacements:
    if old in c:
        c = c.replace(old, new)
        count += 1

with open(f, 'w', encoding='utf-8') as fp:
    fp.write(c)

remaining = c.count('\ufffd')
print(f"Applied {count} replacements. Remaining garbled chars: {remaining}")
if remaining > 0:
    lines = c.split('\n')
    for i, line in enumerate(lines):
        if '\ufffd' in line:
            print(f"  Line {i+1}: {line[:150]}")
