USE drfingerprints;

#Replacing radiochannel table:

DROP TABLE IF EXISTS radiochannel;

CREATE TABLE stations (
	DR_ID    		VARCHAR(19) NOT NULL,
	channel_name    VARCHAR(255) NOT NULL,
    channel_type	VARCHAR(255) NOT NULL,
    streaming_url	VARCHAR(255) NOT NULL,
	PRIMARY KEY (DR_ID)
);


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
	
    INSERT INTO FILES (FILE_PATH, FILE_TYPE) 
		VALUES (parFILE_PATH, parFILE_TYPE);
		
	SET _ID = LAST_INSERT_ID();
	SELECT _ID AS ID;
	END //
DELIMITER ;