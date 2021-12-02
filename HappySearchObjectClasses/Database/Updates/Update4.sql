ALTER TABLE CharacterItems
ADD COLUMN NewSinceUpdate INTEGER DEFAULT 0;

INSERT OR REPLACE INTO TableDetails (Key,Value) VALUES ('updates',4);