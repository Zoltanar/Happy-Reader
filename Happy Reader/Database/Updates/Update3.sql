ALTER TABLE UserGames
ADD COLUMN MatchHookCode INTEGER NOT NULL DEFAULT 0;

INSERT INTO Updates (Id,Timestamp) VALUES (3,datetime());