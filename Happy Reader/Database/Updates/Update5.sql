DELETE FROM GameThreads;
ALTER TABLE GameThreads ADD COLUMN RetnRight INTEGER;
ALTER TABLE GameThreads ADD COLUMN Spl INTEGER;

INSERT INTO Updates (Id,Timestamp) VALUES (5,datetime());