BEGIN TRANSACTION;
CREATE TEMPORARY TABLE ListedVNs_backup (
	`VNID`	INTEGER NOT NULL UNIQUE,
	`Title`	TEXT,
	`KanjiTitle`	TEXT,
	`ReleaseDateString`	TEXT,
	`ProducerID`	INTEGER,
	`Image`	TEXT,
	`ImageNSFW`	INTEGER,
	`Description`	TEXT,
	`LengthTime`	INTEGER,
	`Popularity`	NUMERIC,
	`Rating`	NUMERIC,
	`VoteCount`	INTEGER,
	`Relations`	TEXT,
	`Screens`	TEXT,
	`Anime`	TEXT,
	`Aliases`	TEXT,
	`Languages`	TEXT,
	`ReleaseDate`	DATE,
	`ReleaseLink`	TEXT,
	`TagScore`	REAL,
	`TraitScore`	REAL
);
INSERT INTO ListedVNs_backup SELECT VNID,Title,KanjiTitle,ReleaseDateString,ProducerID,Image,ImageNSFW,Description,LengthTime,Popularity,
				Rating,VoteCount,Relations,Screens,Anime,Aliases,Languages,ReleaseDate,ReleaseLink,TagScore,TraitScore FROM ListedVNs;
DROP TABLE ListedVNs;
CREATE TABLE ListedVNs (
	`VNID`	INTEGER NOT NULL UNIQUE,
	`Title`	TEXT,
	`KanjiTitle`	TEXT,
	`ReleaseDateString`	TEXT,
	`ProducerID`	INTEGER,
	`Image`	TEXT,
	`ImageNSFW`	INTEGER,
	`Description`	TEXT,
	`LengthTime`	INTEGER,
	`Popularity`	NUMERIC,
	`Rating`	NUMERIC,
	`VoteCount`	INTEGER,
	`Relations`	TEXT,
	`Screens`	TEXT,
	`Anime`	TEXT,
	`Aliases`	TEXT,
	`Languages`	TEXT,
	`ReleaseDate`	DATE,
	`ReleaseLink`	TEXT,
	`TagScore`	REAL,
	`TraitScore`	REAL
);
INSERT INTO ListedVNs SELECT VNID,Title,KanjiTitle,ReleaseDateString,ProducerID,Image,ImageNSFW,Description,LengthTime,Popularity,
				Rating,VoteCount,Relations,Screens,Anime,Aliases,Languages,ReleaseDate,ReleaseLink,TagScore,TraitScore FROM ListedVNs_backup;
DROP TABLE ListedVNs_backup;
COMMIT;

INSERT OR REPLACE INTO TableDetails (Key,Value) VALUES ('updates',1);

