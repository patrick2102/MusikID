##Stored procedures for getting radio urls.
DROP PROCEDURE IF EXISTS GET_LIVESTREAM_RESULTS;

DELIMITER //
CREATE PROCEDURE GET_LIVESTREAM_RESULTS(
	IN parSTART TIMESTAMP,
	IN parEND TIMESTAMP,
	IN parCHANNEL_ID VARCHAR(19)
)
 
BEGIN

   SELECT s.id, lr.offset, lr.duration, lr.accuracy, s.dr_diskoteksnr, s.sidenummer, s.sekvensnummer, lr.song_offset, s.duration
   FROM livestream_results lr, songs s
   WHERE (lr.PLAY_DATE BETWEEN parSTART AND parEND)
   AND channel_id = parCHANNEL_ID
   AND lr.song_id = s.id
   ORDER BY lr.PLAY_DATE;
   
   
   END //
DELIMITER ;


##Stored procedures for getting radio urls.
DROP PROCEDURE IF EXISTS GET_ON_DEMAND_RESULTS;

DELIMITER //
CREATE PROCEDURE GET_ON_DEMAND_RESULTS(
	IN parFILE_ID bigint(20)
)
 
BEGIN

   SELECT s.id, lr.offset, lr.duration, lr.accuracy, s.dr_diskoteksnr, s.sidenummer, s.sekvensnummer, lr.song_offset, s.duration
   FROM on_demand_results lr, songs s
   WHERE lr.song_id = s.id
   AND parFILE_ID = lr.FILE_ID
   ORDER BY lr.OFFSET;
   
   END //
DELIMITER ;
<<<<<<< HEAD
<<<<<<< HEAD
=======
=======
>>>>>>> 145_furbergs_fede_ting

##Stored procedures for getting radio urls.
DROP PROCEDURE IF EXISTS GET_ON_DEMAND_FILE;

DELIMITER //
CREATE PROCEDURE GET_ON_DEMAND_FILE(
	IN parFILE_ID bigint(20)
)
 
BEGIN

   SELECT file_path, f.id, j.percentage
	FROM files f, job j
	where f.id = j.file_id
	AND f.id = parFILE_ID
	and job_type LIKE "AudioMatch";
   END //
DELIMITER ;

<<<<<<< HEAD
"System.InvalidCastException: Unable to cast object of type 'System.TimeSpan' to type 'System.DateTime'.\r\n   at DatabaseCommunication.SQLCommunication.GetLivestreamResults(DateTime start, DateTime end, String channel_id, List`1& lst) in C:\\Users\\DREXMAGP\\OneDrive - DR\\Profil\\Desktop\\dr-music-recognition\\AudioFingerprinting-master\\DatabaseCommunication\\SQLCommunication.cs:line 152"

>>>>>>> 145_furbergs_fede_ting
=======
>>>>>>> 145_furbergs_fede_ting
