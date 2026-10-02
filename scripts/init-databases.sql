-- Отдельная БД под AuthService. Основная (POSTGRES_DB = template_service_db)
-- создаётся самим образом postgres; здесь добавляем вторую.
-- Скрипт выполняется только при первой инициализации volume.
CREATE DATABASE auth_service_db;
