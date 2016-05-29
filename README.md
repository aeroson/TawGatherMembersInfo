# TawGatherMembersInfo
Automatic gathering of TAW.net members information for automated processing purposes. 
And automatic generation of TAW.net Arma 3 division squad xmls.

# Arma Squad XMLs:

Automatically generates squad XMLs for all of TAW.net Arma.
Including all members of AM 1 and AM 2.

### What is squad xml
![What is squad xml](http://am2.taw.net/squadxml/what_is_squadxml.png)

### How to get squad xml
![How to get squad xml](http://am2.taw.net/squadxml/how_to_get_squadxml.png)

# API
This project also provides API to automatically get TAW.net members info for example into Google Sheet.
You need to ask AM 2 server staff to give you authentication token.
The information is however easy to get even without this project for anyone (even not logged in users) at http://taw.net/unit/1/roster.aspx
Extra care is taken that it includes all members, even discharged ones, and the order of members always stays the same. (New members are added to the bottom)

http://am2.taw.net:8000/?rootUnitId=1&format=table&type=distinct_person_list&fields=name&orderBy=id&version=3&auth=YOUR_AUTH_TOKEN
Results in:
![Example page result](http://image.prntscr.com/image/70e7118657be4a17810f4b19608930e7.png)

It is a html table that can be imported into Google Sheet, for example for administration purposes.
=IMPORTHTML("http://am2.taw.net:8000/?rootUnitId=1&format=table&type=distinct_person_list&fields=name&orderBy=id&version=3&auth=YOUR_AUTH_TOKEN", "table", 1)

Results in:
![Example google sheet result](http://image.prntscr.com/image/89f57acbb96b41489c61fe08c670fb2a.png)
