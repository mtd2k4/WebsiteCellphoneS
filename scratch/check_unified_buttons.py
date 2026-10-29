import urllib.request
from bs4 import BeautifulSoup

html = urllib.request.urlopen("http://localhost:5016/").read().decode("utf-8")
soup = BeautifulSoup(html, "html.parser")
cont = soup.find("div", id="cpsFloatingButtons")
if cont:
    btns = cont.find_all(class_="cps-float-btn")
    print(f"PASS: Found {len(btns)} buttons in unified cluster!")
    for i, b in enumerate(btns):
        icon = b.find("i")
        icon_class = icon.get("class") if icon else "None"
        print(f"Button {i+1}: classes={b.get('class')} | icon={icon_class}")
else:
    print("FAIL: Container not found!")
