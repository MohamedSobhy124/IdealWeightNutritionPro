-- Combo offer free-delivery settings (ComboOffer.AllowFreeDelivery, FreeDeliveryMinimumAmount).
-- The legacy MVC migration 20261020111700_combo_free_delivery already adds these columns, so on
-- the production database this script normally does nothing. Idempotent; apply by hand.
IF COL_LENGTH('ComboOffers', 'AllowFreeDelivery') IS NULL
    ALTER TABLE [ComboOffers] ADD [AllowFreeDelivery] bit NOT NULL
        CONSTRAINT [DF_ComboOffers_AllowFreeDelivery] DEFAULT (CAST(0 AS bit));
GO

IF COL_LENGTH('ComboOffers', 'FreeDeliveryMinimumAmount') IS NULL
    ALTER TABLE [ComboOffers] ADD [FreeDeliveryMinimumAmount] float NOT NULL
        CONSTRAINT [DF_ComboOffers_FreeDeliveryMinimumAmount] DEFAULT (0);
GO
