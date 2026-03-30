#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 3 ]; then
  echo "Uso: $0 \"mensaje de commit\" ISSUE_NUMBER vX.Y.Z"
  echo "Ejemplo: $0 \"mejora workflow release\" 12 v1.2.3"
  exit 1
fi

COMMIT_MESSAGE="$1"
ISSUE_NUMBER="$2"
VERSION_TAG="$3"

BRANCH_NAME="feature/issue-${ISSUE_NUMBER}-$(date +%Y%m%d%H%M%S)"
FULL_COMMIT_MESSAGE="$COMMIT_MESSAGE

Fixes #$ISSUE_NUMBER"

echo "Creando y cambiando a la rama: $BRANCH_NAME"
git checkout -b "$BRANCH_NAME"

echo "Haciendo add..."
git add -A

echo "Creando commit..."
git commit -m "$FULL_COMMIT_MESSAGE"

echo "Pusheando rama..."
git push -u origin "$BRANCH_NAME"

echo "Creando tag local: $VERSION_TAG"
git tag "$VERSION_TAG"

echo "Intentando pushear tag..."
if git push origin "$VERSION_TAG"; then
  echo "Tag subido correctamente."
else
  echo "AVISO: No se pudo subir el tag $VERSION_TAG."
  echo "Crea el tag después del merge si es necesario."
fi

echo "Listo."
echo "Ahora abre un Pull Request desde $BRANCH_NAME hacia main."