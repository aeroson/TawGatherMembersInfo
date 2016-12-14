delimiter //
drop procedure if exists AttendanceReport//
create procedure AttendanceReport(in rootUnitId bigint(20), in daysBackTo int(10)) -- , in daysBackFrom int(10)
begin
	   
		   
	declare selected_PersonId bigint(20); 
	declare cursor_end tinyint(1);
	
	
	declare AllEventsInvited bigint(20);
	declare AllEventsAttended bigint(20);
	declare AllEventsExcused bigint(20);
	declare AllEventsMissed bigint(20);
			
	declare MandatoryEventsExcused bigint(20);
	declare MandatoryEventsMissed bigint(20);
	declare MandatoryEventsInvited bigint(20);
	declare MandatoryEventsAttended bigint(20);
	
	declare startDate datetime;
	declare endDate datetime;
	
	declare selected_people cursor for select PersonId from GetPeopleInUnit_result where Status not like "discharged%";
	declare continue handler for not found set cursor_end = true;
	
	call GetPeopleInUnit(rootUnitId);
	
	-- this table columns dont really tell us what is the meaning of the numbers
	-- the columns were named the same way they are named on taw.net website attendance report
	create temporary table if not exists attendanceReportResult (
		UnitName varchar(100),
		UserName varchar(500),
		RankNameShort varchar(10),
		Trainings bigint(20),
		Attended bigint(20),
		Excused bigint(20),
		AWOL bigint(20),
		MandatoryAVG float,
		TotalAVG float,
		DaysInRank bigint(20),
		
		AllEventsInvited bigint(20),
		AllEventsAttended bigint(20),
		AllEventsExcused bigint(20),
		AllEventsMissed bigint(20),
		
		MandatoryEventsInvited bigint(20),        
		MandatoryEventsAttended bigint(20),
		MandatoryEventsExcused bigint(20),
		MandatoryEventsMissed bigint(20)		
	);	
	truncate table attendanceReportResult;
	
	-- call GetChildUnits(rootUnitId);

	select (date_sub(now(), interval daysBackTo day)) into startDate;
	
	open selected_people;
	read_loop: LOOP

		fetch selected_people into selected_PersonId;		
				
		if cursor_end then
			leave read_loop;
		end if;     
					
					
		select 
			ifnull(count(*), 0),
			ifnull(sum(case when pe.AttendanceType = 1 then 1 else 0 end), 0),
			ifnull(sum(case when pe.AttendanceType = 2 then 1 else 0 end), 0),
			ifnull(sum(case when pe.AttendanceType = 3 then 1 else 0 end), 0)
		into 
			AllEventsInvited,
			AllEventsAttended,
			AllEventsExcused,
			AllEventsMissed    
		from People p
		join PersonEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId
		join Events e on e.EventId = pe.EventId and e.From > startDate -- and e.Cancelled = false
		;
		
		select 
			ifnull(count(*), 0),
			ifnull(sum(case when pe.AttendanceType = 1 then 1 else 0 end), 0),
			ifnull(sum(case when pe.AttendanceType = 2 then 1 else 0 end), 0),
			ifnull(sum(case when pe.AttendanceType = 3 then 1 else 0 end), 0)
		into 
			MandatoryEventsInvited,            
			MandatoryEventsAttended,
			MandatoryEventsExcused,
			MandatoryEventsMissed	    
		from People p
		join PersonEvents pe on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId
		join Events e on e.EventId = pe.EventId and e.From > startDate and e.Mandatory = true -- and e.Cancelled = false
		-- join UnitEvents ue on ue.Event_EventId = e.EventId and ue.Unit_UnitId in (select * from GetChildUnits_result)
		;
			   
			   
		
		insert into attendanceReportResult values (
		
			(select u.Name from Units u where u.TawId = rootUnitId),
			(select p.Name from People p where p.PersonId = selected_PersonId),
			(select pr.NameShort from People p join PersonRanks pr on p.PersonId = selected_PersonId and pr.Person_PersonId = selected_PersonId order by pr.ValidFrom desc limit 1),			
			AllEventsInvited,			
			AllEventsAttended,			
			AllEventsExcused,			
			AllEventsMissed,
		
			IF(
				MandatoryEventsInvited > 0,
				MandatoryEventsAttended / MandatoryEventsInvited,
				1
			),
			
			IF(
				AllEventsInvited > 0,
				AllEventsAttended / AllEventsInvited,
				1
			),
			
			(select datediff(CURRENT_DATE, pr.ValidFrom) from People p join PersonRanks pr on p.PersonId = selected_PersonId and pr.Person_PersonId = selected_PersonId order by pr.ValidFrom desc limit 1),
			
			AllEventsInvited,
			AllEventsAttended,
			AllEventsExcused,
			AllEventsMissed,
			
			MandatoryEventsInvited,			
			MandatoryEventsAttended,
			MandatoryEventsExcused,
			MandatoryEventsMissed		
			
		);
		

	end loop;
	
	close selected_people;
	
	select * from attendanceReportResult order by UserName;


end//
delimiter ;
