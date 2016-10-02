select * from 
	(SELECT count(*) as Commendations FROM Commendations) a,
	(SELECT count(*) as Events FROM Events) b,
    (SELECT count(*) as People FROM People) c,
	(SELECT count(*) PersonCommendationComments from PersonCommendationComments) d,
	(SELECT count(*) PersonCommendations from PersonCommendations) e,
	(SELECT count(*) PersonEvents from PersonEvents) f,
	(SELECT count(*) PersonRanks from PersonRanks) g,
	(SELECT count(*) PersonUnits from PersonUnits) h,
	(SELECT count(*) UnitEvents from UnitEvents) i,
	(SELECT count(*) Units from Units) j
;