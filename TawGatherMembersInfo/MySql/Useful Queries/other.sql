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

call AttendanceReport(2776, 2000);


select * from People where name like "bigboom";
select * from People;

select count(*),type from Units group by type;



delete from UnitEvents;
delete from PersonEvents;
delete from Events;
ALTER TABLE Events AUTO_INCREMENT = 1;

select * from Events order by TawId desc limit 10;


select * from People p where p.name = "Aleksander";
