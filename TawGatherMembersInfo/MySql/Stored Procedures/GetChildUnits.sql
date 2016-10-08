delimiter //
drop procedure if exists GetChildUnits//
create procedure GetChildUnits(in rootUnitId bigint(20))
begin
	
	create temporary table if not exists GetChildUnits_result (UnitId bigint(20));
	truncate table GetChildUnits_result;

	insert into GetChildUnits_result 
	select distinct a.UnitId from
	(
		select u1.UnitId from Units u1 where u1.TawId = rootUnitId
	) a
	union all
	(
		select u2.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
	)
	union all
	(
		select u3.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
	)
	union all
	(
		select u4.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
			join Units u4 on u4.ParentUnit_UnitId = u3.UnitId
	)
	union all
	(
		select u5.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
			join Units u4 on u4.ParentUnit_UnitId = u3.UnitId
			join Units u5 on u5.ParentUnit_UnitId = u4.UnitId
	)
	union all
	(
		select u6.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
			join Units u4 on u4.ParentUnit_UnitId = u3.UnitId
			join Units u5 on u5.ParentUnit_UnitId = u4.UnitId
			join Units u6 on u6.ParentUnit_UnitId = u5.UnitId
	)
	union all
	(
		select u7.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
			join Units u4 on u4.ParentUnit_UnitId = u3.UnitId
			join Units u5 on u5.ParentUnit_UnitId = u4.UnitId
			join Units u6 on u6.ParentUnit_UnitId = u5.UnitId
			join Units u7 on u7.ParentUnit_UnitId = u6.UnitId
	)
	union all
	(
		select u8.UnitId from Units u1
			join Units u2 on u2.ParentUnit_UnitId = u1.UnitId and u1.TawId = rootUnitId 
			join Units u3 on u3.ParentUnit_UnitId = u2.UnitId
			join Units u4 on u4.ParentUnit_UnitId = u3.UnitId
			join Units u5 on u5.ParentUnit_UnitId = u4.UnitId
			join Units u6 on u6.ParentUnit_UnitId = u5.UnitId
			join Units u7 on u7.ParentUnit_UnitId = u6.UnitId
			join Units u8 on u8.ParentUnit_UnitId = u7.UnitId
	);

end//
delimiter ;
