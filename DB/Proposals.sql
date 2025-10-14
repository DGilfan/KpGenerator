BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "Proposals" (
	"Id"	INTEGER,
	"CustomerName"	TEXT,
	"Area"	REAL,
	"Design"	INTEGER
, CustumerType TEXT, CreatedAt TEXT);
INSERT INTO "Proposals" ("Id","CustomerName","Area","Design","CustumerType","CreatedAt") VALUES (NULL,'Иванов Иван Иванович',65.0,1,NULL,NULL),
 (NULL,'Петров ПетрПетрович',75.0,0,NULL,NULL),
 (NULL,'Сидоров Сидор Сидорович',99.0,1,NULL,NULL),
 (NULL,'Николаев Николай Николаевич',18.0,0,NULL,NULL);
COMMIT;
