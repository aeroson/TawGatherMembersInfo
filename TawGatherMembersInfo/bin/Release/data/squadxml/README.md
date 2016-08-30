# Custom squad xml logo
If you want to select custom squad xml logo append this at the bottom of your TAW profile: 
```
squadxml logo: medic
```
Where the "medic" is the name of the picture form this folder.

Example:

![Where to append the setting](http://image.prntscr.com/image/987090c5428f447795b735497363aaea.png)

# Custom Arma profile name

This project is capable to correctly compose teamspeak names only for Arma divison members.
Teamspeak names of personel from other divisions may not be correct.
Append this at the bottom of your TAW profile to specify your Arma ingame profile name (which should be same as your Teamspeak name).
```
arma profile name: Hadesdaman [SOCOP]
```
# Squadxml data folder
This is the folder where all squadxmls are generated into. And where all squad logos reside.
Images are bound to the unit TAW id. http://taw.net/unit/2776/roster.aspx the number 2776 in the link is the unit taw id.
You can also use the -child suffix to give squad logo to all child(descendant) units.
If you add new squad logo please also regenerate .paa Arma images accordingly. 
You can use the "convert all .paa.png to .paa .bat" for that. You need Arma 3 Tools installed (and launched at least once) for it to work. You can get Arma 3 Tools from Steam > Library > Tools.
