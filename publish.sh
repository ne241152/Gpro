#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"

SOURCE="Gpro_web_fallback_NEW"
DEST="docs"

echo "WebGLビルドを確認中..."

# 空のフォルダからの誤コピーを防止
if [ ! -s "$SOURCE/index.html" ] ||
   [ ! -d "$SOURCE/Build" ] ||
   [ ! -d "$SOURCE/TemplateData" ]; then
    echo "エラー：ビルドファイルが不足しています"
    exit 1
fi

shopt -s nullglob
for ext in data.unityweb framework.js.unityweb wasm.unityweb loader.js; do
    files=("$SOURCE"/Build/*."$ext")
    if [ "${#files[@]}" -ne 1 ] || [ ! -s "${files[0]}" ]; then
        echo "エラー：$ext がありません"
        exit 1
    fi
    if ! grep -Fq "$(basename "${files[0]}")" "$SOURCE/index.html"; then
        echo "エラー：index.html の参照先が一致しません"
        exit 1
    fi
done

# Gitの状態確認
if [ "$(git branch --show-current)" != "main" ]; then
    echo "エラー：mainブランチで実行してください"
    exit 1
fi

if ! git diff --cached --quiet ||
   ! git diff --quiet -- "$DEST/"; then
    echo "エラー：ステージ済みの変更、またはdocs内の未コミット変更があります"
    exit 1
fi

git fetch origin main

if [ "$(git rev-parse HEAD)" != "$(git rev-parse origin/main)" ]; then
    echo "エラー：mainが最新ではありません"
    exit 1
fi

# 公開ファイルを更新
rsync -av --delete \
  --exclude='.nojekyll' \
  --exclude='.DS_Store' \
  "$SOURCE/" "$DEST/"

touch "$DEST/.nojekyll"

# GitHubへ送信
git add -A docs/

if git diff --cached --quiet; then
    echo "公開ファイルに変更はありません"
    exit 0
fi

git commit -m "Update WebGL build"
git push origin main

echo "公開データの送信が完了しました！"
