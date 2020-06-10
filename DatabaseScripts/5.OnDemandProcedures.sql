use DRFINGERPRINTS;

DROP TABLE IF EXISTS ON_DEMAND_RESULTS;
DROP TABLE IF EXISTS FILES;

CREATE TABLE IF NOT EXISTS FILES (
	ID BIGINT NOT NULL AUTO_INCREMENT,
	FILE_PATH VARCHAR(256) NOT NULL,
	FILE_TYPE VARCHAR(10) NOT NULL,
    PRIMARY KEY(ID)
);


CREATE TABLE IF NOT EXISTS ON_DEMAND_RESULTS (
    ID BIGINT NOT NULL AUTO_INCREMENT,
    SONG_ID INT NOT NULL,
    LAST_UPDATED TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6), 
    OFFSET TIME NOT NULL,
    DURATION INT NOT NULL,
    FILE_ID BIGINT NOT NULL,
    ACCURACY FLOAT NOT NULL,
    PRIMARY KEY(ID),
    FOREIGN KEY (SONG_ID) REFERENCES SONGS(ID),
	FOREIGN KEY (FILE_ID) REFERENCES FILES(ID)
);



##Stored procedures for inserting into ON_DEMAND_RESULTS
DROP PROCEDURE IF EXISTS INSERT_ON_DEMAND_RESULTS;

DELIMITER //
CREATE PROCEDURE INSERT_ON_DEMAND_RESULTS(
    IN parDR_DISKOTEKSNR INT,
    IN parSIDENUMMER INT,
    IN parSEKVENSNUMMER INT,
    IN parOFFSET TIME,
    IN parDURATION INT,
	IN parFILE_ID BIGINT,
    IN parACCURACY FLOAT,
	IN parSONG_OFFSET FLOAT
)
 
BEGIN
  DECLARE TRACK_ID INT;

  SET TRACK_ID = NULL;
  
   SELECT ID INTO TRACK_ID
	FROM SONGS S
	WHERE
		parDR_DISKOTEKSNR = S.DR_DISKOTEKSNR AND
		parSIDENUMMER = S.SIDENUMMER AND
		parSEKVENSNUMMER = S.SEKVENSNUMMER;
   
   INSERT INTO ON_DEMAND_RESULTS (
	SONG_ID,
	OFFSET, 
	DURATION,
	FILE_ID,
	ACCURACY,
	SONG_OFFSET
    ) 
	VALUES ( 
	TRACK_ID,
	parOFFSET, 
	parDURATION,
    parFILE_ID,
	parACCURACY,
	parSONG_OFFSET);

   SET TRACK_ID = LAST_INSERT_ID();
   SELECT TRACK_ID AS ID;
	
   END //
DELIMITER ;


DROP PROCEDURE IF EXISTS UPDATE_ON_DEMAND_RESULTS;
DELIMITER //
    CREATE PROCEDURE UPDATE_ON_DEMAND_RESULTS(
	IN parID INT,
	IN parFILE_ID INT,
	IN parOFFSET TIME,
	IN parDURATION INT,
	IN parACCURACY FLOAT
        )
	BEGIN
    UPDATE ON_DEMAND_RESULTS SET 
		OFFSET = parOFFSET,
        DURATION = parDURATION,
        ACCURACY = parACCURACY
	WHERE parID = ID AND parFILE_ID = FILE_ID;
	
	END //
 DELIMITER ;
 
DROP PROCEDURE IF EXISTS INSERT_FILE;
DELIMITER //
    CREATE PROCEDURE INSERT_FILE(
	IN parFILE_PATH VARCHAR(256),
	IN parFILE_TYPE VARCHAR(10),
    IN parREFERENCE VARCHAR(50)
    )
	BEGIN
	
	DECLARE _ID INT;
	SET _ID = NULL;
	
    INSERT INTO FILES (FILE_PATH, FILE_TYPE, ref) 
		VALUES (parFILE_PATH, parFILE_TYPE, parREFERENCE);
		
	SET _ID = LAST_INSERT_ID();
	SELECT _ID AS ID;
	END //
DELIMITER ;