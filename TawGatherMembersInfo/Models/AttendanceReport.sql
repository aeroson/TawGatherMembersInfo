delimiter //
drop procedure if exists AttendanceReport//
create procedure AttendanceReport(in rootUnitId bigint(20), in daysBack int(10))
begin
	   
    declare selected_PersonId bigint(20);    
    declare cursor_end tinyint(1);
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

	DECLARE CONTINUE HANDLER FOR NOT FOUND SET cursor_end = 1;
    
	open selected_people;
	REPEAT

		FETCH selected_people INTO selected_PersonId;		
        		
        -- WHERE exec_datetime BETWEEN DATE_SUB(NOW(), INTERVAL daysBack DAY) AND NOW();        
		
        select 
        
			(select u.Name from Units u where u.TawId = rootUnitId) as UnitName,
            (select p.Name from People p where p.PersonId = selected_PersonId) as UserName,
            (select p.RankNameShort from People p where p.PersonId = selected_PersonId) as RankNameShort,                      
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId) as Trainings,
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1) as Attended,
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 2) as Excused,   
            
            (select count(*) from People p join PeopleToEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 3) as AWOL,
		
			(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1 join Events e on e.Mandatory and e.EventId = pe.EventId) -- total mandatories attended
			/ (select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.Mandatory and e.EventId = pe.EventId) -- total mandatories
			as MandatoryAVG,
			
			(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId and pe.AttendanceType = 1) -- total any events attended
			/ (select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId) -- total any events
			as TotalAVG,
			
            0 as DaysInRank
		;
        
		UNTIL cursor_end = 1
	END REPEAT;
    close selected_people;
    


end//
delimiter ;
