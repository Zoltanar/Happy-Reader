﻿ALTER TABLE Entrys
ADD COLUMN GameIdIsUserGame INTEGER NOT NULL DEFAULT 0;

INSERT INTO Updates (Id,Timestamp) VALUES (1,datetime());