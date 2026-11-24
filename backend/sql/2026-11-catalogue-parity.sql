-- Phase 4 catalogue parity schema changes (Product.ExpiryDate, ComboOfferImages).
-- Idempotent; the project has no EF migrations (legacy DB), so apply this by hand.
IF COL_LENGTH('Products', 'ExpiryDate') IS NULL
    ALTER TABLE [Products] ADD [ExpiryDate] datetime2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComboOfferImages')
BEGIN
    CREATE TABLE [ComboOfferImages] (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ComboOfferImages] PRIMARY KEY,
        [ComboOfferId] int NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [DisplayOrder] int NOT NULL CONSTRAINT [DF_ComboOfferImages_DisplayOrder] DEFAULT (0),
        [ImageInfo] nvarchar(500) NULL,
        CONSTRAINT [FK_ComboOfferImages_ComboOffers_ComboOfferId] FOREIGN KEY ([ComboOfferId])
            REFERENCES [ComboOffers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ComboOfferImages_ComboOfferId] ON [ComboOfferImages] ([ComboOfferId]);
END
GO
