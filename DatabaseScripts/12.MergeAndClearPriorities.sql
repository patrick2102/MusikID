
-- THIS IS NOT USED ANYMORE!!!!!!!!



USE DRFINGERPRINTS;

##Stored procedures for Merging all data in newsong into songs and clearing newsongs afterwards
DROP PROCEDURE IF EXISTS CLEAR_AND_MERGE_NEW_SONGS_PRIORITY;

DELIMITER //
CREATE PROCEDURE CLEAR_AND_MERGE_NEW_SONGS_PRIORITY()
 
BEGIN
	
	INSERT IGNORE INTO songs (ID, DR_DISKOTEKSNR, SIDENUMMER, SEKVENSNUMMER, REFERENCE, DURATION) 
	SELECT ID, DR_DISKOTEKSNR, SIDENUMMER, SEKVENSNUMMER, REFERENCE, DURATION
	FROM newsongs
	Where ispriority;
	
	delete from newsongs
	where ispriority = 0;
   END //
DELIMITER ;



 -- INSERT INTO NEWSONGS (    
               -- REFERENCE,
               -- DR_DISKOTEKSNR,
               -- SIDENUMMER,
               -- SEKVENSNUMMER,
               -- DURATION,
			   -- ISPRIORITY
               -- )
    -- VALUES(	-1,
			-- -1,
            -- -1,
            -- -1,
			-- 2,
			-- FALSE);
			
			
			ALTER TABLE on_demand_results ALTER COLUMN duration SET DEFAULT -1;
			
			