# Markdown File

In case the connection to a cloud mongo DB like
mongodb://<dbuser>:<dbpassword>@ds052629.mlab.com:52629/realestate
is not working =>
we fail back to the locally installed mongodb server:

cd c:\program files\mongodb\server\3.4\bin
mongod --dbpath ../data
we need to have previously created a sibling subfolder 'data' of the 'bin' subfolder

install robomongo as UI Admin tool for the MongoDB (robomongo.org)

we could also use a mongod.cfg configuration file