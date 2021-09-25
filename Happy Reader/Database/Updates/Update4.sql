ALTER TABLE UserGames
ADD COLUMN Note TEXT;

INSERT INTO Updates (Id,Timestamp) VALUES (4,datetime());