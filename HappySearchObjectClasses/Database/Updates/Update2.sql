ALTER TABLE UserVNs
ADD COLUMN LastModified DATETIME;

INSERT OR REPLACE INTO TableDetails (Key,Value) VALUES ('updates',2);