select * from People p where name = "JamesHeywood" limit 10;

select * from PersonRanks pr where pr.Person_PersonId = 1839 order by pr.ValidFrom desc;

select * from PersonRanks pr where pr.TawId = 0 ;

select * from Events e where e.TawId = 40430;
select * from PersonRanks where TawId = 3978;
select * from Units where TawId = 3978;

select count(*) NameShort from PersonRanks group by NameShort;


delete from PersonRanks where NameShort = "Unknown" and PromotedBy_PersonId > 0;

call GetChildUnits(2776);
select * from GetChildUnits_result;

call GetPeopleInUnit(2776);
select * from GetPeopleInUnit_result;

call AttendanceReport(2776, 30);

select * from People p order by p.PersonId desc limit 10;

select * from People where name = "Deceded";

select * from People p join PersonUnits pu on p.PersonId = pu.Person_PersonId and p.Name = "Wingnut"
join Units u on u.UnitId = pu.Unit_UnitId;

select * from People p join PersonEvents pe on p.PersonId = pe.PersonId and p.Name = "Wingnut";

update PersonUnits pu set pu.Removed = '9999-01-01 00:00:00' where pu.Removed < '0001-01-01 00:00:00';

update PersonUnits pu set pu.Removed = '9999-01-01 00:00:00' where pu.Removed = '0000-00-00 00:00:00';

select * from Events e where e.EventId = 13099;


select count(*) from PersonUnits pu where pu.Removed = '0000-00-00 00:00:00';
select * from PersonUnits pu where pu.Removed < '0001-01-01 00:00:00' limit 10;
select * from People p where p.AdmittedToTaw = null limit 10;
select * from PersonUnits pu order by pu.PersonUnitId desc limit 10;

select * from PersonUnits pu where pu.Unit_UnitId = 590;


select now();

select count(*),type from Units group by type;



delete from UnitEvents;
delete from PersonEvents;
delete from Events;
ALTER TABLE Events AUTO_INCREMENT = 1;

select * from Events order by TawId desc limit 10;


