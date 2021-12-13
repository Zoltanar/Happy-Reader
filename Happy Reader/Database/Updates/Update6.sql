ALTER TABLE CachedTranslations
ADD COLUMN GameId INTEGER DEFAULT 0;
ALTER TABLE CachedTranslations
ADD COLUMN IsUserGame INTEGER DEFAULT 0;

INSERT INTO Updates (Id,Timestamp) VALUES (6,datetime());