ALTER TABLE UserGames
ADD COLUMN LaunchModeOverride INTEGER NOT NULL DEFAULT 0;

INSERT INTO Updates (Id,Timestamp) VALUES (2,datetime());