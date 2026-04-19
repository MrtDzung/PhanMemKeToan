import sys
sys.stdout.reconfigure(encoding='utf-8')
f = r'F:\DuAn\PhanMemKeToan\src\webapp\src\app\features\di\inventory-items\components\inventory-item-form\inventory-item-form.component.html'
with open(f,'r',encoding='utf-8') as fp: c=fp.read()
fixes = [
    ('B?t bu?c', 'Bắt buộc'),
    ('Thu? su?t GTGT (%)', 'Thuế suất GTGT (%)'),
    ('Ho?t d?ng', 'Hoạt động'),
    ('T? l? quy d?i', 'Tỷ lệ quy đổi'),
    ('Ch?n m?u định mức', 'Chọn mẫu định mức'),
    ('M?c t?n t?i thi?u', 'Mức tồn tối thiểu'),
    ('M?c t?n t?i da', 'Mức tồn tối đa'),
]
count=0
for old,new in fixes:
    if old in c:
        c=c.replace(old,new); count+=1
with open(f,'w',encoding='utf-8') as fp: fp.write(c)
print(f'Applied {count} fixes')
for i,line in enumerate(c.split('\n')):
    if '?' in line:
        print(f'Line {i+1}: {line[:150]}')
