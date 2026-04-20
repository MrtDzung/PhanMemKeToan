import sys
import os
sys.stdout.reconfigure(encoding='utf-8')

f = r"F:\DuAn\PhanMemKeToan\src\webapp\src\app\features\di\inventory-items\components\inventory-item-form\inventory-item-form.component.html"
with open(f, 'r', encoding='utf-8') as fp:
    c = fp.read()

REPL = '\ufffd'
lines = c.split('\n')
for i, line in enumerate(lines):
    if REPL in line:
        print(f"{i+1}: {line[:150]}")
print(f"\nTotal garbled chars: {c.count(REPL)}")
