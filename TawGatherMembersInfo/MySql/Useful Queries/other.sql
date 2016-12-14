select * from People p where name = "JamesHeywood" limit 10;

select * from PersonRanks pr where pr.Person_PersonId = 1839 order by pr.ValidFrom desc;

select * from PersonRanks pr where pr.TawId = 0 ;

select * from Events e where e.TawId = 40430;
select * from PersonRanks where TawId = 3978;
select * from Units where TawId = 3978;

select p.LastProfileDataUpdatedDate from People p where p.Name = "Dackey";

select count(*) NameShort from PersonRanks group by NameShort;

call GetChildUnits(2776);
select * from GetChildUnits_result;

call GetPeopleInUnit(2776);
select * from GetPeopleInUnit_result;

call AttendanceReport(2776, 30);
call AttendanceReport(1330, 300);
call AttendanceReport(2947, 30);

select * from People p order by p.PersonId desc limit 10;

select * from People where name = "Curt";

select u.*, pu.*,  p.* from People p join PersonUnits pu on p.PersonId = pu.Person_PersonId and p.Name = "dostojetski" join Units u on u.UnitId = pu.Unit_UnitId;

select pr.* from People p join PersonRanks pr on p.PersonId = pr.Person_PersonId and p.Name = "Dackey" order by ValidFrom desc;
select pe.*, e.* from People p join PersonEvents pe on p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and p.Name = "Pepsimax";
select e.*, pe.* from People p join PersonEvents pe on p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > (date_sub(now(), interval 30 day)) and p.Name = "Bazoon";

update PersonUnits pu set pu.Removed = '9999-01-01 00:00:00' where pu.Removed < '0001-01-01 00:00:00';

update PersonUnits pu set pu.Removed = '9999-01-01 00:00:00' where pu.Removed = '0000-00-00 00:00:00';

select * from Events e where e.EventId = 13099;

select * from PersonRanks where TawId = 0;

select * from People where Name = "dostojetski";

select * from Events e where e.TawId = 1189;
select e.* from Events e order by e.EventId desc limit 100;
select e.* from Events e order by e.TawId desc limit 100;
select e.* from Events e order by e.From limit 100;
select e.* from Events e where e.Name like "[AM 2%" order by e.TawId desc limit 100;
select * from Events e where e.Name = null;

select count(*) from PersonUnits pu where pu.Removed = '0000-00-00 00:00:00';
select * from PersonUnits pu where pu.Removed < '0001-01-01 00:00:00' limit 10;
select * from People p where p.AdmittedToTaw = null limit 10;
select * from PersonUnits pu order by pu.PersonUnitId desc limit 10;
select * from PersonUnits pu where pu.Unit_UnitId = 590;

select * from Events e where e.TawId = 74124;
select pe.* from Events e join PersonEvents pe on e.EventId = pe.EventId and e.TawId = 74124;
select p.*, pe.* from Events e join PersonEvents pe on e.EventId = pe.EventId and e.TawId = 74124 join People p on p.PersonId = pe.PersonId;


select now();

select count(*),type from Units group by type;

select * from PersonRanks where length(NameShort) > 3;


ALTER TABLE Events AUTO_INCREMENT = 1;

select * from Events order by TawId desc limit 10;



