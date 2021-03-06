CREATE DATABASE IF NOT EXISTS DRFINGERPRINTS;
USE DRFINGERPRINTS;
DROP TABLE IF EXISTS SONGS;
DROP TABLE IF EXISTS FINGERID;
DROP TABLE IF EXISTS SUBFINGERID;
DROP TABLE IF EXISTS RADIOCHANNEL;

CREATE TABLE SONGS (
	ID						INT NOT NULL AUTO_INCREMENT, /* Possibly delete later*/
	DR_DISKOTEKSNR 			INT NOT NULL,
    SIDENUMMER				INT NOT NULL,
    SEKVENSNUMMER			INT NOT NULL,
    DATE_CHANGED			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    REFERENCE				VARCHAR(20) NOT NULL,
    AUDIO_FORMAT			VARCHAR(12) NOT NULL,
    SONG_NAME				VARCHAR(256) NOT NULL,
    DURATION				BIGINT NOT NULL DEFAULT -1,
    PRIMARY KEY (ID),
	UNIQUE KEY DK1_SONGS (REFERENCE)
);

CREATE TABLE FINGERID (
	ID 						INT NOT NULL AUTO_INCREMENT,
    DATE_CHANGED			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    DATE_ADDED				DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SIGNATURE				LONGBLOB NOT NULL,
	PRIMARY KEY (ID),
	KEY DK1_FINGERID (DATE_ADDED)
);

CREATE TABLE SUBFINGERID (
	ID 						INT NOT NULL AUTO_INCREMENT,
    DATE_CHANGED			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    DATE_ADDED				DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SIGNATURE				LONGBLOB NOT NULL,
	PRIMARY KEY (ID),
	KEY DK1_SUBFINGERID (DATE_ADDED)
);

CREATE TABLE RADIOCHANNEL (
	RADIOCHANNEL_ID    		INT NOT NULL AUTO_INCREMENT,
	RADIONAME          		VARCHAR(50) NOT NULL,
	NAMECODE           		VARCHAR(40) NOT NULL,
	URL                		VARCHAR(127) NOT NULL,
	PRIMARY KEY (RADIOCHANNEL_ID)
);

/*
// Copyright (c) 2015-2017 Stichting Centrale Discotheek Rotterdam.
// 
// website: https://www.muziekweb.nl
// e-mail:  info@muziekweb.nl
//
// This code is under MIT licence, you can find the complete file here: 
// LICENSE.MIT
*/

-- ------------------------------------------------------------------------------------------
-- Needed SP's
-- ------------------------------------------------------------------------------------------

/*
DELIMITER $$

DROP PROCEDURE IF EXISTS RADIOCHANNEL_S$$

CREATE PROCEDURE RADIOCHANNEL_S (
    IN parNAMECODE    VARCHAR(40)
)
exit_proc:BEGIN
  SELECT *
  FROM   RADIOCHANNEL
  WHERE  NAMECODE = parNAMECODE;
 
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS DELETEFINGERINDEX$$

CREATE PROCEDURE DELETEFINGERINDEX (
    IN parDATEGEWIJZIGD              DATE,
    IN parMAX_TITELNUMMERTRACK_ID    INT
)
exit_proc:BEGIN
  SELECT *
  FROM   TITELNUMMERTRACK_ID
  WHERE  TITELNUMMERTRACK_ID <= parMAX_TITELNUMMERTRACK_ID  
  AND    GEWIJZIGD > (parDATEGEWIJZIGD + INTERVAL 1 DAY)
  ORDER BY GEWIJZIGD;
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS DELETESUBFINGERINDEX$$

CREATE PROCEDURE DELETESUBFINGERINDEX (
    IN parDATEGEWIJZIGD              DATE,
    IN parMAX_TITELNUMMERTRACK_ID    INT
)
exit_proc:BEGIN
  SELECT *
  FROM   TITELNUMMERTRACK_ID
  WHERE  TITELNUMMERTRACK_ID <= parMAX_TITELNUMMERTRACK_ID  
  AND    GEWIJZIGD > (parDATEGEWIJZIGD + INTERVAL 1 DAY)
  ORDER BY GEWIJZIGD;
END$$


*/
DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS FINGERID_D$$

CREATE PROCEDURE FINGERID_D (
    IN parREFERENCE     VARCHAR(20)
)
exit_proc:BEGIN
-- YN 2015-08-18
  DECLARE _TRACK_ID    INT;
  
  SET _TRACK_ID = NULL;
  
  SELECT ID
           INTO _TRACK_ID
  FROM   SONGS
  WHERE  REFERENCE = parREFERENCE;

  IF _TRACK_ID IS NOT NULL THEN
    DELETE FROM FINGERID WHERE ID = _TRACK_ID;
  END IF;
END$$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS FINGERID_IU$$

CREATE PROCEDURE FINGERID_IU (
    IN parREFERENCE     VARCHAR(20),
    IN parAUDIO_FORMAT          VARCHAR(12),
    IN parDURATION		        BIGINT,
    IN parSONG_NAME              VARCHAR(256),
    IN parSIGNATURE            LONGBLOB
)
exit_proc:BEGIN
-- YN 2015-08-21
-- FINGERID is een ACOUSTID fingerprint van de gehele track
--
  DECLARE _TRACK_ID    INT;

  SET _TRACK_ID = NULL;


  SELECT ID
           INTO _TRACK_ID
  FROM   SONGS
  WHERE  REFERENCE = parREFERENCE;

  IF _TRACK_ID IS NULL THEN
  
    INSERT INTO SONGS (    
               REFERENCE,
               AUDIO_FORMAT,
               SONG_NAME,
               DURATION
               )
    VALUES(parREFERENCE,
           parAUDIO_FORMAT,
           parSONG_NAME,
           parDURATION);
    
    SET _TRACK_ID = LAST_INSERT_ID();

  ELSE

    UPDATE SONGS
      SET AUDIO_FORMAT= IFNULL(parAUDIO_FORMAT, AUDIO_FORMAT),
          SONG_NAME = IFNULL(parSONG_NAME, SONG_NAME),
          DURATION = IFNULL(parDURATION, DURATION)
    WHERE ID = _TRACK_ID;

  END IF;

  DELETE FROM FINGERID
  WHERE ID = _TRACK_ID;

  INSERT INTO FINGERID (    
              ID,
              SIGNATURE)
  VALUES(_TRACK_ID,
         parSIGNATURE); 
  SELECT _TRACK_ID AS ID;
  
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS SUBFINGERID_D$$

CREATE PROCEDURE SUBFINGERID_D (
    IN parREFERENCE     VARCHAR(20)
)
exit_proc:BEGIN
-- YN 2015-08-18
  DECLARE _TRACK_ID    INT;

  SET _TRACK_ID = NULL;

  
  SELECT ID
           INTO _TRACK_ID
  FROM   SONGS
  WHERE  REFERENCE = parREFERENCE;

  IF _TRACK_ID IS NOT NULL THEN
    DELETE FROM SUBFINGERID WHERE ID = _TRACK_ID;
  END IF;

END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS SUBFINGERID_IU$$

CREATE PROCEDURE SUBFINGERID_IU (
    IN parREFERENCE     	VARCHAR(20),
    IN parAUDIO_FORMAT      VARCHAR(12),
    IN parDURATION       	BIGINT,
    IN parSONG_NAME        	VARCHAR(256),
    IN parSIGNATURE         LONGBLOB
)
exit_proc:BEGIN
-- YN 2015-08-21
-- 
  DECLARE _TRACK_ID    INT;

  SET _TRACK_ID = NULL;
  
  SELECT ID
           INTO _TRACK_ID
  FROM   SONGS
  WHERE  REFERENCE = parREFERENCE;

  IF _TRACK_ID IS NULL THEN

    INSERT INTO SONGS (    
               REFERENCE,
               AUDIO_FORMAT,
               SONG_NAME,
               DURATION
               )
    VALUES(parREFERENCE,
           parAUDIO_FORMAT,
           parSONG_NAME,
           parDURATION);
    
    SET _TRACK_ID = LAST_INSERT_ID();

  ELSE

    UPDATE SONGS
      SET AUDIO_FORMAT= IFNULL(parAUDIO_FORMAT, AUDIO_FORMAT),
          SONG_NAME = IFNULL(parSONG_NAME, SONG_NAME),
          DURATION = IFNULL(parDURATION, DURATION)
    WHERE ID = _TRACK_ID;

  END IF;

  DELETE FROM SUBFINGERID
  WHERE ID = _TRACK_ID;

  INSERT INTO SUBFINGERID (    
              ID,
              SIGNATURE)
  VALUES(_TRACK_ID,
         parSIGNATURE); 
  SELECT _TRACK_ID AS ID;

END$$

DELIMITER ;

/*
DELIMITER $$

DROP PROCEDURE IF EXISTS TITELNUMMERTRACK_S$$

CREATE PROCEDURE TITELNUMMERTRACK_S (
    IN parTITELNUMMERTRACK_ID    BIGINT
)
exit_proc:BEGIN
-- YN 2015-08-18
  SELECT *
  FROM   TITELNUMMERTRACK_ID 
  WHERE  TITELNUMMERTRACK_ID = parTITELNUMMERTRACK_ID;
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS TITELNUMMERTRACK_S2$$

CREATE PROCEDURE TITELNUMMERTRACK_S2 (
    IN parTITELNUMMERTRACK    VARCHAR(20)
)
exit_proc:BEGIN
-- YN 2015-08-18
  SELECT *
  FROM   TITELNUMMERTRACK_ID
  WHERE  TITELNUMMERTRACK = parTITELNUMMERTRACK;
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS `UPDATEDFINGERINDEX`$$

CREATE PROCEDURE UPDATEDFINGERINDEX (
    IN parDATEGEWIJZIGD    DATE
)
exit_proc:BEGIN
  SELECT *
  FROM   TITELNUMMERTRACK_ID
  WHERE  GEWIJZIGD > (parDATEGEWIJZIGD + INTERVAL 1 DAY)
  ORDER BY GEWIJZIGD;
END$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS UPDATEDSUBFINGERINDEX$$

CREATE PROCEDURE UPDATEDSUBFINGERINDEX (
    IN parDATEGEWIJZIGD    DATE
)
exit_proc:BEGIN
  SELECT *
  FROM   TITELNUMMERTRACK_ID
  WHERE  GEWIJZIGD > (parDATEGEWIJZIGD + INTERVAL 1 DAY)
  ORDER BY GEWIJZIGD;
END$$

DELIMITER ;

*/
