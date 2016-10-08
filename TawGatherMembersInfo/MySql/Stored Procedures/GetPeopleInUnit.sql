delimiter //
drop procedure if exists GetPeopleInUnit//
create procedure GetPeopleInUnit(in rootUnitTawId bigint(20))
begin
	
    call GetChildUnits(rootUnitTawId);
    
	create temporary table if not exists GetPeopleInUnit_result like People;
	truncate table GetPeopleInUnit_result;

	insert into GetPeopleInUnit_result 
		select p.* from People p        
		join PersonUnits pu on pu.Person_PersonId = p.PersonId and pu.Removed > now() and pu.Unit_UnitId in
		(	
			select * from GetChildUnits_result
		)
		group by p.PersonId;

end//
delimiter ;
