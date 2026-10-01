#!/usr/bin/env bash
# Создаёт новый сервис из шаблона: копирует проект и переименовывает
# TemplateService / template-service / template_service / template.
#
# Usage:
#   ./scripts/create-service.sh <ServiceName> <TargetDir>
#
# Example:
#   ./scripts/create-service.sh BillingService ../BillingService
set -euo pipefail

if [ $# -ne 2 ]; then
    echo "Usage: $0 <ServiceName> <TargetDir>" >&2
    exit 1
fi

SERVICE_NAME="$1"
TARGET_DIR="$2"

if ! [[ "$SERVICE_NAME" =~ ^[A-Z][A-Za-z0-9]*$ ]]; then
    echo "ServiceName must be PascalCase (letters and digits), e.g. BillingService" >&2
    exit 1
fi

KEBAB=$(printf '%s' "$SERVICE_NAME" | sed -E 's/([a-z0-9])([A-Z])/\1-\2/g' | tr '[:upper:]' '[:lower:]')
SNAKE=$(printf '%s' "$KEBAB" | tr '-' '_')
PASCAL_BASE="${SERVICE_NAME%Service}"
[ -z "$PASCAL_BASE" ] && PASCAL_BASE="$SERVICE_NAME"
BASE=$(printf '%s' "$PASCAL_BASE" | tr '[:upper:]' '[:lower:]')

SOURCE_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

TARGET_DIR="$(cd -- "$(dirname -- "$TARGET_DIR")" && pwd)/$(basename -- "$TARGET_DIR")"

if [ -e "$TARGET_DIR" ]; then
    echo "Target already exists: $TARGET_DIR" >&2
    exit 1
fi

case "$TARGET_DIR/" in
    "$SOURCE_DIR"/*)
        echo "Target cannot be inside the template: $TARGET_DIR" >&2
        exit 1
        ;;
esac

echo "Creating $SERVICE_NAME in $TARGET_DIR"
echo "  PascalCase: $SERVICE_NAME"
echo "  kebab-case: $KEBAB"
echo "  snake_case: $SNAKE"
echo "  base word:  $BASE"
echo

mkdir -p "$TARGET_DIR"

# 1. Копируем, исключая артефакты сборки и секреты.
if command -v rsync >/dev/null 2>&1; then
    rsync -a \
        --exclude '.git' \
        --exclude 'node_modules' \
        --exclude '.next' \
        --exclude 'bin' \
        --exclude 'obj' \
        --exclude '*.tsbuildinfo' \
        --exclude '.env' \
        "$SOURCE_DIR"/ "$TARGET_DIR"/
else
    tar -C "$SOURCE_DIR" \
        --exclude '.git' \
        --exclude 'node_modules' \
        --exclude '.next' \
        --exclude 'bin' \
        --exclude 'obj' \
        --exclude '*.tsbuildinfo' \
        --exclude '.env' \
        -cf - . | tar -C "$TARGET_DIR" -xf -
fi

# 2. Переименовываем файлы и папки, в именах которых встречается template.
# find -depth отдаёт детей раньше родителей, поэтому сначала переименовывается
# basename файла, затем — его папок.
while IFS= read -r -d '' path; do
    dir="$(dirname -- "$path")"
    base="$(basename -- "$path")"
    new_base="$base"
    new_base="${new_base//TemplateService/$SERVICE_NAME}"
    new_base="${new_base//template-service/$KEBAB}"
    new_base="${new_base//template_service/$SNAKE}"
    new_base="${new_base//Template/$PASCAL_BASE}"
    new_base="${new_base//template/$BASE}"

    if [ "$base" != "$new_base" ]; then
        mv "$path" "$dir/$new_base"
    fi
done < <(find "$TARGET_DIR" -depth \( -name '*TemplateService*' -o -name '*Template*' -o -name '*template*' \) -print0)

# 3. Меняем содержимое текстовых файлов.
# Имена файлов в шаблоне без переводов строк — построчный read безопасен.
while IFS= read -r file; do
    perl -pi -e "
        s/TemplateService/$SERVICE_NAME/g;
        s/Template Service/$PASCAL_BASE Service/g;
        s/template-service/$KEBAB/g;
        s/template_service/$SNAKE/g;
        s/Template/$PASCAL_BASE/g;
        s/template/$BASE/g;
    " "$file"
done < <(grep -rIl -e 'TemplateService' -e 'Template Service' -e 'template-service' -e 'template_service' -e 'Template' -e 'template' "$TARGET_DIR" || true)

echo "Done."
echo
echo "Next steps:"
echo "  cd $TARGET_DIR"
echo "  cp .env.example .env   # NUGET_*, AUTH_AUTHORITY, PostgreSQL"
echo "  docker compose up -d --build"
echo
echo "Не забудь зарегистрировать audience '$KEBAB' (scope/resource) в AuthService"
echo "и обновить .env приложения."
