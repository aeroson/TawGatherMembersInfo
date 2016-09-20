delimiter //
drop procedure if exists AttendanceReport//
create procedure AttendanceReport(in rootUnitId bigint(20), in daysBack int(10))
begin
	   
	       
    declare selected_PersonId bigint(20); 
    declare cursor_end tinyint(1);
    
    declare totalMandatories bigint(20);
    declare totalAnyEvent bigint(20);
    
	declare selected_people cursor for
		select p.PersonId from People p
		join PeopleToUnits p2u on p2u.PersonId = p.PersonId and p2u.UnitId in
		(
			select * from
				(select battalion.UnitId from Units battalion where battalion.TawId = rootUnitId) a
			union all
				(select platoon.UnitId from Units battalion
				join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId)
			union all
				(select squad.UnitId from Units battalion
				join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId 
				join Units squad on platoon.UnitId = squad.ParentUnit_UnitId)
			union all
				(select fireteam.UnitId from Units battalion
				join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId 
				join Units squad on platoon.UnitId = squad.ParentUnit_UnitId
				join Units fireteam on squad.UnitId = fireteam.ParentUnit_UnitId)
		)
		group by p.PersonId
		order by name;
        
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET cursor_end = true;
    
	CREATE TEMPORARY TABLE IF NOT EXISTS attendanceReportResult (
		UnitName varchar(100),
        UserName varchar(500),
        RankNameShort varchar(10),
        Trainings bigint(20),
        Attended bigint(20),
        Excused bigint(20),
        AWOL bigint(20),
        MandatoryAVG float,
		TotalAVG float,
        DaysInRank bigint(20)
	);
    
    TRUNCATE TABLE attendanceReportResult;
    
	open selected_people;
	read_loop: LOOP

		FETCH selected_people INTO selected_PersonId;		
        		
		IF cursor_end THEN
			leave read_loop;
		END IF;                
                
        -- WHERE exec_datetime BETWEEN DATE_SUB(NOW(), INTERVAL daysBack DAY) AND NOW();        
        
        select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.Mandatory and e.EventId = pe.EventId into totalMandatories;
        select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId into totalAnyEvent;
		
        insert into attendanceReportResult values (
        
			(select u.Name from Units u where u.TawId = rootUnitId),
            (select p.Name from People p where p.PersonId = selected_PersonId),
            (select p.RankNameShort from People p where p.PersonId = selected_PersonId),                      
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId),
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1),
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 2),
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 3),
		
			IF(
				totalMandatories > 0,
				(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1 join Events e on e.Mandatory and e.EventId = pe.EventId) / totalMandatories,
				0
			),
			
            IF(
				totalAnyEvent > 0,
				(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1) / totalAnyEvent,
				0
			),
			
            0
		);
	end loop;
    close selected_people;
    
    select * from attendanceReportResult order by UserName;


end//
delimiter ;
