\ This Gforth code is a sqlite3 wrapper library 
\    Copyright (C) 2013  Philip King. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.

\ Note the following is needed on your Debian (tested with wheezy):
\ sudo apt-get install sqlite3 libsqlite3-dev
\ These apt-get installs are needed to connect this code to sqlite3 code

\ How to use this library as follows:
\ -First simply include it into your Gforth code like normal:
\ include sqlite3_gforth_lib.fs
\ -Now each database is simply a file as sqlite3 is an embeded database that is how it is done.
\  This means you give the path of the file to this code to work on that database:
\ s" /full-path-to-your-db-file/dbname-here" dbname
\ -Next you form your SQL statement to get some data from the database:
\ s" select * from mytable ;" dbcmds
\ - Next you issue give the info to sqlite3 to get your results:
\ sendsqlite3cmd throw
\  Note sendsqlite3cmd will return an error if one happens or false (0) if no error
\ - Now get the results back for processing in any way you see fit:
\ dbret$
\   This simply returns a counted string that has the results returned from your SQL querie
\   Note not all queries result in a response so you need to understand SQL to understand how this is used.
\ -If you want more error information use the following:
\ dberrmsg
\   This will return the same error number that sendsqlite3cmd returns with also a counted string with error message.
\ -You will note the SQL return string will have fields and records separated with ',' and cr.
\   To change these separator strings use the follwoing:
\ s" **" dbfieldseparator
\   This makes the field separator "**"
\ s" &" dbrecordseparator
\   This makes the record separator "&"
\   Note there is a special response from SQL called null.  This is handled such that if null is ever returned
\   it is returned as "NULL".  So do not use NULL in your datasets or as a separator string or your code
\   will not know when a real null is encountered in your datasets!
\ -To prevend memory leaks this code allocates room for the return string and manages that string.
\   Sometimes you will find the 200 bytes allocated will not be enough for the returned SQL result.
\   This will cause an error "Return buffer to samll to reiceve all strings from sqlite3!" message.
\   This can be fixed by enlarging the return buffer as follows:
\ 1000 mkretbuff
\   In fact you can make the return buffer any size you want as long as gforth has that memory to allocate or you will get
\   errors from Gforth about memory issues.  This example sets the buffer to 1000 bytes.  Note after this resizing is done
\   the buffer will stay that size until you change it or your program restarts and then the default 200 bytes will be used.
\ -The return strings from dbret$ will only return the amount of the string from SQLITE3 so remember that last part of that
\   string will be the record separator proceeded by the field separator. 

include string.fs

clear-libs
c-library mysqlite3

s" sqlite3" add-lib

\c #include <stdio.h>
\c #include "sqlite3.h"
\c #include <string.h>
\c
\c char * theBuffer = 0;
\c int maxBuffsize = 0;
\c char * fseparator = 0;
\c char * rseparator = 0;
\c int bufferok =0;
\c
\c static int callback( void * NotUsed, int argc, char ** argv, char ** azColName ) {
\c int i;
\c char temp[maxBuffsize] ;
\c int tempSize;
\c int tempBuffsize;
\c
\  Note that this function will return the string "NULL" when there is a null in the db field 
\c for(i = 0; i < argc ; i++) {
\c      sprintf(temp,"%s",argv[i] ? argv[i] : "NULL");
\c      strcat(temp,fseparator);    
\c      tempSize = strlen(temp);
\c      tempBuffsize = strlen(theBuffer);
\c      if((tempBuffsize + tempSize + 1) < maxBuffsize){
\c           strcat(theBuffer,temp);
\c           } else {
\c           bufferok = 1;
\c           return 0; }
\c      }
\c if((strlen(theBuffer) + strlen(rseparator) + 1) < maxBuffsize) {
\c    strcat(theBuffer,rseparator);
\c    } else {
\c    bufferok = 1; }
\c return 0;
\ This was from testing but i left it here to remember the variable meanings!
\ \c for(i=0; i < argc ; i++) {
\ \c    printf( "%s = %s\n", azColName[i], argv[i] ? argv[i] : "NULL" );
\ \c    }
\c }
\  filename is a string for the filename of the database.
\  sqlite3_cmds is a string of the commands to send to sqlite3.
\  sqlite3_ermsg is the returned string of errors if any happen.
\  buffer is a string that will contain the result of the sqlite3 commands if any results are expected!
\  buffsize is the size or the buffer string including room for the null terminator
\  sep is a string that contains the field and record seperator to incert into the returned result in buffer.
\  buffok is a char string of dimention one that contains 0 or 1 depending on if the result returned fit in the buffsize of buffer.
\  Note all these strings are need to be zero terminated at entry to this function.
\  Note this function will return 0 when it can connect to named database
\  All other numbers returned are errors #s directly recieved from sqlite3_exec() function!
\  Error messages from sqlite3 will always be returned in the sqlite3_ermsg string.
\c 
\c int sqlite3to4th( const char * filename, char * sqlite3_cmds, char * sqlite3_ermsg , char * buffer, int buffsize , char * fsep, char * rsep, char * buffok) {
\c sqlite3 *db;
\c char * zErrMsg = 0 ;
\c int rc = 0 ;
\c theBuffer = buffer;
\c maxBuffsize = buffsize;
\c fseparator = fsep;
\c rseparator = rsep;
\c buffok[0] = 0;
\c bufferok = 0;
\c
\c rc = sqlite3_open( filename, &db ) ;
\c if( rc ) {
\c    sprintf( sqlite3_ermsg,"SQL error: Can't open database: %s\n", sqlite3_errmsg( db ) );
\c    sqlite3_close( db );
\c    return (rc);
\c }
\c    
\c rc = sqlite3_exec( db, sqlite3_cmds, callback, 0, &zErrMsg );
\c if( rc!=SQLITE_OK ) {
\c   sprintf( sqlite3_ermsg, "SQL error: %s\n", zErrMsg );
\c   sqlite3_free( zErrMsg );
\c }
\c sqlite3_close( db );
\c if(bufferok==1) buffok[0]=1;
\c return (rc) ;
\c }
\c

\ **** sqlite3 gforth wrappers ****

c-function sqlite3 sqlite3to4th a a a a n a a a -- n
\ note that c strings are always null terminated unlike gforth strings! 
    
end-c-library
\ list of errors this code can produce apart from the sqlite3 library it self.
\ note this gets enumerated once you call this library
struct
    cell% field retbuffover-err
    cell% field retbuffover$
end-struct sqlerrors%
create sqlerrors
sqlerrors% %allot drop
s" Return buffer to small to recieve all strings from sqlite3!" sqlerrors retbuffover$ $!
sqlerrors retbuffover$ $@ exception sqlerrors retbuffover-err !

: -NULL$ ( caddr u -- caddr u1 ) \ searchs for 0 or NULL in sting and when found reduces count of string to not include NULL
    2dup s"  " over 0 swap c! search if swap drop - else 2drop then ;

: z$! ( caddr u addr1 -- ) \ works with $! from string.fs but adds a null at end of string to pass to c code
    swap 1 + swap dup { pointer } $! \ note $! will free allocated memory if it needs to
    0 pointer $@ 1 - + c! ;

: z$@ ( caddr -- caddr1 ) \ returns the pointer to char ready for c code to use!
    $@ drop ;  \ remember caddr should be the address of a z$! creation so it should have a null at the end of the string

struct
    cell% field dbname-$
    cell% field dbcmds-$
    cell% field dberrors-$
    cell% field retbuff-$
    cell% field retbuffmaxsize-cell
    cell% field fseparator-$
    cell% field rseparator-$
    char% field buffok-flag
    cell% field error-cell
end-struct sqlite3message%

create sqlmessg
sqlite3message%  %allot drop

: mkretbuff ( nsize -- )
    sqlmessg retbuffmaxsize-cell !
    sqlmessg retbuffmaxsize-cell @ allocate throw { addr } 
    addr sqlmessg retbuffmaxsize-cell @ erase
    addr sqlmessg retbuffmaxsize-cell @ sqlmessg retbuff-$ z$!
    addr free throw ;

200 mkretbuff \ start the return buffer at 200 bytes for now

: mkerrorbuff ( -- )
    80 allocate throw { addr }
    addr 80 erase
    addr 80 sqlmessg dberrors-$ z$!
    addr free throw ;

: initsqlbuffers ( -- ) \ clear only the buffers to use sqlite3 but not the name or the cmds strings
    mkerrorbuff
    sqlmessg retbuffmaxsize-cell @ mkretbuff \ clear the return buffer but not resize it!
    0 sqlmessg buffok-flag c! ; 

: initsqlall ( -- ) \ clear all sqlmessg data 
    s" " sqlmessg dbname-$ z$!
    s" " sqlmessg dbcmds-$ z$!
    s" ," sqlmessg fseparator-$ z$!   \ set field separator to a comma
    s\" \n" sqlmessg rseparator-$ z$! \ set record seperatore to linefeed 
    initsqlbuffers ;

initsqlall \ structure now has allocated memory

: dbname ( caddr u -- )  \ set the db name with string
    sqlmessg dbname-$ z$! ;

: dbcmds ( caddr u -- ) \ the string for the db commands to send sqlite3
    sqlmessg dbcmds-$ z$! ;

: dbfieldseparator ( caddr u -- ) \ this string is the returned field separator used 
    sqlmessg fseparator-$ z$! ;

: dbrecordseparator ( caddr u -- ) \ this string is the returned record separator used
    sqlmessg rseparator-$ z$! ;

: sendsqlite3cmd ( -- nerror ) \ will send the commands to sqlite3 and nerror contains false if no errors
    TRY
	initsqlbuffers
	sqlmessg dbname-$ z$@
        sqlmessg dbcmds-$ z$@
	sqlmessg dberrors-$ z$@
	sqlmessg retbuff-$ z$@
	sqlmessg retbuffmaxsize-cell @
	sqlmessg fseparator-$ z$@
	sqlmessg rseparator-$ z$@
	sqlmessg buffok-flag 
	sqlite3 dup sqlmessg error-cell !
	dup 0=
	if sqlmessg buffok-flag c@ 0<>
	    if
		drop sqlerrors retbuffover$ $@ sqlmessg dberrors-$ z$!
		sqlerrors retbuffover-err @ throw
	    then
	then
    RESTORE 
    ENDTRY ;

: dberrmsg ( -- caddr u nerror )  \ note the nerror is only 0 if sqlite3 connected to database otherwise it is the sqlite3 error number.
    \ Nerror can also return the sqlerrors retbuffover-err value meaning that return message did not fit in the buffer but only if sqlite3 retuned 0!
    \ The caddr u will contain any error messages from sqlite3 itself and nerror should be either sqlite3 error#s or retbufferover-err value or 0!
    sqlmessg dberrors-$ $@ -NULL$ sqlmessg error-cell @ ;

: dbret$ ( -- caddr u ) \
    sqlmessg retbuff-$ $@ -NULL$ ;

