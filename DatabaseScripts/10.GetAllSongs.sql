DONT DELETE THIS OR CLOSE


QUERY FOR CHECKING ROLLING WINDOW

select * from livestream_results where CHANNEL_ID = "P8J" and PLAY_DATE between "2019-05-03 17:45:00" and "2019-05-03 18:05:00";

QUERY FOR LOOKING AT P3 RADIO

select s.id, reference, lr.play_date, lr.last_updated, lr.DURATION, lr.accuracy from livestream_results lr, songs s where lr.song_id = s.id and (lr.CHANNEL_ID = "P3") order by lr.LAST_UPDATED desc, lr.CHANNEL_ID desc limit 20;

PATH TO SONG WE CANT FIND

"\\\\musa01\\download\\ITU\\MUR\\Missingfiles\\9016577-1-4_Rihanna_Work.wav"


ROLLING WINDOW  COMMAND : 

-rw 01/05/2019-14:30:00 01/05/2019-14:36:00 P3



ALTER TABLE job
ADD COLUMN arguments VARCHAR(100) AFTER percentage;


{
  "startTime": "01/05/2019 14:30:00",
  "endTime": "01/05/2019 14:36:00",
  "radioID": "P3"
}

ALTER TABLE job MODIFY COLUMN arguments VARCHAR(500);

ALTER TABLE job CHANGE COLUMN arguments ARGUMENTS VARCHAR(150);

ALTER TABLE job MODIFY ARGUMENTS varchar(500) AFTER LAST_UPDATED;


"\\\\musa01\\download\\ITU\\MUR\\Dropfolder\\200562519_P6_2019-04-27_12-13_P6_BEAT_elsker_D-A-D.wav"

{
  "startTime": "03/05/2019 11:09:30",
  "endTime": "03/05/2019 11:13:30",
  "radioID": "P3"
}



SET foreign_key_checks = 0;

UPDATE stations 
SET 
    DR_ID = "P1D"
WHERE
    DR_ID = "P1";

UPDATE livestream_results set CHANNEL_ID = "P1D" where CHANNEL_ID = "P1";

UPDATE stations 
SET 
    DR_ID = "P2D"
WHERE
    DR_ID = "P2";
	
UPDATE livestream_results set CHANNEL_ID = "P2D" where CHANNEL_ID = "P2";
	
UPDATE stations 
SET 
    DR_ID = "KH4"
WHERE
    DR_ID = "RKH";
	
UPDATE livestream_results set CHANNEL_ID = "KH4" where CHANNEL_ID = "RKH";
	
SET foreign_key_checks = 1;
	
	
	
	
SET foreign_key_checks = 0;	
	
UPDATE songs
SET 
    id = -1
WHERE
    id = 13862 && duration = -1;

UPDATE livestream_results set song_id = -1 where song_id = 13862;
UPDATE on_demand_results set song_id = -1 where song_id = 13862;

SET foreign_key_checks = 1;
	
	
	
ffmpeg -i "\\musa01\download\ITU\MUR\MissingFiles\2212169-1-7_D.A.D._Girl Nation.wav" -ar 44100 -ac 1 2212169-1-7_D.A.D._Girl Nation.wav