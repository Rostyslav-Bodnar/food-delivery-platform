-- Створюємо базу, якщо її ще немає
CREATE DATABASE IF NOT EXISTS PromoServiceDb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE PromoServiceDb;

-- Таблиця промокодів
CREATE TABLE IF NOT EXISTS promos (
                                      id BIGINT AUTO_INCREMENT PRIMARY KEY,
                                      code VARCHAR(255) NOT NULL UNIQUE,
    discount_percent INT NOT NULL,
    valid_from DATETIME NULL,
    valid_until DATETIME NULL,
    restaurant_id INT NULL,
    category_id INT NULL,
    is_global BOOLEAN DEFAULT FALSE,
    usage_limit INT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
    );

-- Таблиця використань промокодів
CREATE TABLE IF NOT EXISTS promo_usages (
                                            id BIGINT AUTO_INCREMENT PRIMARY KEY,
                                            promo_id BIGINT NOT NULL,
                                            account_id BIGINT NOT NULL,
                                            used_at DATETIME NULL,
                                            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                            CONSTRAINT fk_promo FOREIGN KEY (promo_id) REFERENCES promos(id)
    );

-- Індекс для швидкої перевірки чи користувач вже використав промо
CREATE UNIQUE INDEX idx_promo_user ON promo_usages(promo_id, account_id);
