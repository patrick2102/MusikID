USE DRFINGERPRINTS;



##Stored procedures for getting radio url from DR_ID
DROP PROCEDURE IF EXISTS GET_RADIO_URL_FROM_ID;

DELIMITER //
CREATE PROCEDURE GET_RADIO_URL_FROM_ID(
    IN CHANNEL_ID varchar(19)
)
 
BEGIN

   SELECT streaming_url
	FROM stations
	WHERE
		DR_ID = CHANNEL_ID
	
   END //
DELIMITER ;

##Stored procedures for getting radio urls.
DROP PROCEDURE IF EXISTS GET_RADIO_URLS;

DELIMITER //
CREATE PROCEDURE GET_RADIO_URLS(
)
 
BEGIN

   SELECT DR_ID, streaming_url
	FROM stations
	
   END //
DELIMITER ;



DROP PROCEDURE IF EXISTS GET_FILES;

DELIMITER //
CREATE PROCEDURE GET_FILES(
)
 
BEGIN

   SELECT file_path, id
	FROM files;
	
   END //
DELIMITER ;

DROP PROCEDURE IF EXISTS GET_ON_DEMAND_FILES;

DELIMITER //
CREATE PROCEDURE GET_ON_DEMAND_FILES(
)
 
BEGIN

   SELECT file_path, f.id, j.percentage
	FROM files f, job j
	where f.id = j.file_id
	and job_type LIKE "AudioMatch";
	
   END //
DELIMITER ;

	
