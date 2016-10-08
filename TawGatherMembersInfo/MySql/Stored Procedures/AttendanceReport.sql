delimiter //
drop procedure if exists AttendanceReport//
create procedure AttendanceReport(in rootUnitId bigint(20), in daysBackTo int(10)) -- , in daysBackFrom int(10)
begin
	   
		   
	declare selected_PersonId bigint(20); 
	declare cursor_end tinyint(1);
	
	
	declare totalInvitedAnyEvent bigint(20);
    declare totalAttendedAnyEvent bigint(20);
	declare totalExcusedAnyEvent bigint(20);
	declare totalAwolAnyEvent bigint(20);
            
	declare totalExcusedMandatories bigint(20);
	declare totalAwolMandatories bigint(20);
	declare totalInvitedMandatories bigint(20);
	declare totalAttendedMandatories bigint(20);
	
	declare startDate datetime;
	declare endDate datetime;
	
	declare selected_people cursor for select PersonId from GetPeopleInUnit_result;
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
		DaysInRank bigint(20)
	);	
	truncate table attendanceReportResult;
    
	call GetChildUnits(rootUnitId);

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
            ifnull(sum(case when pe.AttendanceType = 3 then 1 else 0 end), 0),
            
            ifnull(sum(case when e.Mandatory then 1 else 0 end), 0),
            ifnull(sum(case when pe.AttendanceType = 1 and e.Mandatory then 1 else 0 end), 0),
            ifnull(sum(case when pe.AttendanceType = 2 and e.Mandatory then 1 else 0 end), 0),
            ifnull(sum(case when pe.AttendanceType = 3 and e.Mandatory then 1 else 0 end), 0)
    
		into 
			totalInvitedAnyEvent,
            totalAttendedAnyEvent,
            totalExcusedAnyEvent,
            totalAwolAnyEvent,
            
			totalInvitedMandatories,            
            totalAttendedMandatories,
			totalExcusedMandatories,
			totalAwolMandatories	
    
        from PersonEvents pe 
        join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId
        join Events e on e.EventId = pe.EventId and e.From > startDate
        join PersonUnits pu on pu.Person_PersonId = p.PersonId and pu.Removed > now() and pu.Unit_UnitId in (select * from GetChildUnits_result)
        ;
               
        
		insert into attendanceReportResult values (
		
			(select u.Name from Units u where u.TawId = rootUnitId),
			(select p.Name from People p where p.PersonId = selected_PersonId),
			(select pr.NameShort from People p join PersonRanks pr on p.PersonId = selected_PersonId and pr.Person_PersonId = selected_PersonId order by pr.ValidFrom desc limit 1),			
			totalInvitedAnyEvent,			
			totalAttendedAnyEvent,			
			totalExcusedAnyEvent,			
			totalAwolAnyEvent,
		
			IF(
				totalInvitedMandatories > 0,
				totalAttendedMandatories / totalInvitedMandatories,
				1
			),
			
			IF(
				totalInvitedAnyEvent > 0,
				totalAttendedAnyEvent / totalInvitedAnyEvent,
				1
			),
			
			(select datediff(CURRENT_DATE, pr.ValidFrom) from People p join PersonRanks pr on p.PersonId = selected_PersonId and pr.Person_PersonId = selected_PersonId order by pr.ValidFrom desc limit 1)
            
		);
        

	end loop;
	
	close selected_people;
	
	select * from attendanceReportResult order by UserName;


end//
delimiter ;
