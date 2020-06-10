USE DRFINGERPRINTS;

##Stored procedures for Merging all data in newsong into songs and clearing newsongs afterwards
DROP PROCEDURE IF EXISTS CLEAR_AND_MERGE_NEW_SONGS;

DELIMITER //
CREATE PROCEDURE CLEAR_AND_MERGE_NEW_SONGS()
 
BEGIN
	
	INSERT INTO songs (ID, DR_DISKOTEKNR, SIDENUMMER, SEKVENSNUMMER, DATE_CHANGED, REFERENCE, DURATION)
	SELECT (ID, DR_DISKOTEKNR, SIDENUMMER, SEKVENSNUMMER, DATE_CHANGED, REFERENCE, DURATION) 
	FROM newsongs;
	
	TRUNCATE newsongs;
	
	SET @max = (SELECT MAX(id) + 1 from songs);
	SET @s = CONCAT('ALTER TABLE NEWSONGS AUTO_INCREMENT = ', @max);
	PREPARE stm FROM @s;
	EXECUTE stm;
	DEALLOCATE PREPARE stm;

   END //
DELIMITER ;

ID						INT NOT NULL AUTO_INCREMENT, /* Possibly delete later*/
	DR_DISKOTEKSNR 			INT NOT NULL, /*DR_DISKOTEKNR*/
    SIDENUMMER				INT NOT NULL, /*SIDENUMBER*/
    SEKVENSNUMMER			INT NOT NULL, /*SEQUENCENUMBER*/
    DATE_CHANGED			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    REFERENCE				VARCHAR(20) NOT NULL,
    DURATION				BIGINT NOT NULL DEFAULT -1,
	ISPRIORITY 