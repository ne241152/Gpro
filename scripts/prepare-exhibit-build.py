#!/usr/bin/env python3
"""Unity 6で作ったWebGLビルドを、Gpro展示テスト版に組み立てる。
使い方: python3 scripts/prepare-exhibit-build.py ../Gpro_exhibit_build
"""
import argparse
import re
import shutil
from pathlib import Path

root = Path(__file__).resolve().parent.parent
dest = root / "docs" / "exhibit-test"
tpl = root / "Gpro_web_fallback_NEW"
parser = argparse.ArgumentParser()
parser.add_argument("build", type=Path)
parser.add_argument("--replace", action="store_true")
args = parser.parse_args()
src = args.build.expanduser().resolve()

if not (src / "Build").is_dir() or not (src / "TemplateData").is_dir():
    parser.error("Unity WebGLのBuild/TemplateDataが見つかりません")
if src == dest.resolve():
    parser.error("Build出力先はdocs/exhibit-testの外にしてください")
if dest.exists() and not args.replace:
    parser.error("docs/exhibit-testが存在します。更新時は--replaceを指定してください")

names = {}
for key, pattern in (
    ("loaderUrl", "*.loader.js"),
    ("dataUrl", "*.data.unityweb"),
    ("frameworkUrl", "*.framework.js.unityweb"),
    ("codeUrl", "*.wasm.unityweb"),
):
    matches = list((src / "Build").glob(pattern))
    if len(matches) != 1:
        parser.error(f"{key}: {pattern} が1つ必要です。Decompression Fallbackも確認してください")
    names[key] = matches[0].name

html = (tpl / "index.html").read_text(encoding="utf-8")
for key, name in names.items():
    pattern = r'(\b' + key + r'\s*(?:=|:)\s*buildUrl\s*\+\s*["\']/)[^"\']+(["\'])'
    html, count = re.subn(pattern, lambda m: m.group(1) + name + m.group(2),
                          html, count=1)
    if count != 1:
        parser.error("Unity用HTMLの変更箇所が見つかりません: " + key)
if "window.GproUnityInstance = unityInstance;" not in html:
    parser.error("Unity通知の設定がありません")
if 'src="passkey-exhibit.js"' not in html:
    parser.error("認証用JavaScriptの設定がありません")

register = (root / "docs/passkey-test/index.html").read_text(encoding="utf-8")
old = "https://ne241152.github.io/Gpro/"
if old not in register:
    parser.error("登録ページの遷移先が見つかりません")
register = register.replace(old, "https://ne241152.github.io/Gpro/exhibit-test/")
# 展示の登録ページは登録ボタンのみ。独立したログインテストUIは除外する。
login_section_start = register.find('\\n<div style="margin-top:32px">\\n  <button id="login-button"')
login_section_end = register.rfind('\\n</body>')
if login_section_start < 0 or login_section_end < login_section_start:
    parser.error("登録ページのログインテスト領域が見つかりません")
register = register[:login_section_start] + register[login_section_end:]

if dest.exists():
    shutil.rmtree(dest)
shutil.copytree(src, dest, ignore=shutil.ignore_patterns(".DS_Store"))
(dest / "index.html").write_text(html, encoding="utf-8")
shutil.copy2(tpl / "passkey-exhibit.js", dest / "passkey-exhibit.js")
(dest / "register.html").write_text(register, encoding="utf-8")
print("展示テスト版の作成完了:", dest)
print("QRコード用: https://ne241152.github.io/Gpro/exhibit-test/register.html")
print("注意: まだGitHub Pagesには公開していません")
