\ This Gforth code part of my library of linux tools!
\    Copyright (C) 2013  Philip King Smith

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

[IFUNDEF] semaphore-constants

clear-libs
c-library mysemaphore
s" rt" add-lib
\c #include <semaphore.h> 
\c #include <sys/stat.h>
\c #include <fcntl.h>

\c sem_t * info_sem_t(){ return(SEM_FAILED); }
\c int info_oflag(){ return(O_CREAT | O_EXCL); }

c-function sem-info-sem_t info_sem_t -- a
c-function sem-info-oflag info_oflag -- n
c-function sem-open sem_open a n n n -- a
c-function sem-openexisting sem_open a n -- a 
c-function sem-close sem_close a -- n
c-function sem-unlink sem_unlink a -- n
c-function sem-getvalue sem_getvalue a a -- n
c-function sem-dec sem_wait a -- n
c-function sem-inc sem_post a -- n
c-function sem-trydec sem_trywait a -- n

end-c-library

\ *************************************************
\ The following words access or make a semaphore that you need to manage the handle for yourself.
\ Note some words will clober pad in there use!
\ *************************************************
\ Use the following words like this:
\ variable mysemapointer
\ s" asema" 872 semaphore-mk-named [if] drop [then] mysemapointer @ [then]
\ mysemapointer @ semaphore@ [if] drop s" semaphore can't be retrieved!" type [else] s"Semaphore value is " type . [then]
\ Basically you can see that you need to manage the storage of the semaphore pointer and the string name for the semaphore in your own way! See below for a structure that does this somewhat for you!

: *char ( caddr u -- caddr ) \ note this clobers pad up to u + 2 elements 
    dup 2 + pad swap erase pad swap move pad  ;

\ This is used to get SEM_FAILED system pointer and the oflag values for use with semaphores.
: semaphore-constants ( -- noflag asem_t* ) \ returns oflag values, mode_t values and SEM_FAILED value for use with semaphores below.
    sem-info-oflag sem-info-sem_t ;   

\ This is to open an existing named semaphore in caddr u.  You get a sem_t* to access semaphore until you close it so save this value to access it!
: semaphore-op-existing ( caddr u -- asem_t* nflag ) \ nflag is false if semaphore was made and asem_t* is now pointer to semaphore
    *char
    0 sem-openexisting dup
    semaphore-constants swap drop  = ;

\ This is to create a new named semaphore with a starting value.  You will get sem_t* pointer to access the semaphore until you close it so save this value!
: semaphore-mk-named ( caddr u nvalue -- asem_t* nflag ) \ nflag is false if semaphore was made and asem_t* is now pointer to semaphore
    >r *char
    semaphore-constants r> swap >r 436 swap 
    sem-open dup r> = ;

\ This is to access the named semaphore value but you need the sem_t* pointer received during open!
: semaphore@ ( asem_t* -- nvalue nflag ) \ nflag is false if nvalue is valid.  nvalue is semaphore value.  Note pad is clobered. 
    0 pad ! pad sem-getvalue pad @ swap ;

\ This is to close access to semaphore pointed to by asem_t*
: semaphore-close ( asem_t* -- nflag ) \ nflag is false if semaphore was closed
    sem-close ;

\ This is to delete the semaphore from the system.
: semaphore-delete ( caddr u -- nflag ) \ nflag is false if semaphore was removed without errors
    *char sem-unlink ;

\ This is to add one to a semaphores value.
: semaphore+ ( asem_t* -- nflag ) \ nflag is false if semaphore was incremented by one.
    sem-inc ;

\ This is to reduce a semaphores value by one but it blocks so see below for non blocking decrement.
: semaphore- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one.  Note this blocks if semaphore value is zero upon entry.
    sem-dec ;

\ This decrements a semaphores value by one and if that can't happen an error will be returned so it is not blocking.
: semaphore-try- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one. Note this does not block as semaphore- does but will return error if failed to decrement semaphore.
    sem-trydec ;

\ *************************************************************
\ The following words allow treating a semaphore something like a variable in gforth
\ *************************************************************
\ use as follows:
\ sema% asemaphore           \ makes structure to hold semaphore stuff named "asemaphore" in dictionary.  Will return asema% or the address of the structure when used!
\ s" asemaphore" $sema%      \ you can use this to create the semaphore structure aslo.  This allows you to use caps in semaphore name but remember gforth stores names without caps so the created structure can be accessed without caps. 
\ 593 asemaphore sema-mk-named . \ you will show false if asemaphore was created with value 593 in it.
\ asemaphore sema@ . .       \ will return 0 593.  The 0 shows the next value is the real value from the semaphore. 593 is the value asemaphore contains now!
\ asemaphore sema-close .    \ will return 0.  This means this process does not have access to the semaphore anymore but the structure is still in memory!
\ asemaphore sema-op-exist . \ will return 0.  This means the semaphore called "asemaphore" was opened for use by this process.
\ asemaphore sema- .         \ will return 0 when it decrements the semaphore as it is blocking.
\ asemaphore sema-try- .     \ will return 0 when it decrements "asemaphore" or non zero meaning decrement of asemaphore did not happen.
\ asemaphore sema+ .         \ will return 0 when asemaphore is incremented.
\ asemaphore sema-delete .   \ will return false if asemaphore was deleted from the system.
\ Note as seamphores are system variables other processes can do things to them also so these commands will be cued by the system properly and then acted on in proper time!

: sema% ( compilation. "semaphore-name" -- : run-time. -- asema% ) \ use this to make a semaphore structure
    \ sem_t* nvalue "semaphore-name" (the "semaphore-name" is stored here with null terminator for transfering to c code)
    \ Note the structure holds the semaphore name for c transfer and also has place for pointer to semaphore and a cell for value passing.
    CREATE sem-info-sem_t dup 2, latest name>string dup dup 1+ here dup rot allot rot erase swap cmove  ;

: $sema% ( compilation. caddr u -- : run-time. -- asema% )
    \ caddr u is a string containing the name of the created structure and the semaphore name that the system will create later.
    \ the null terminator will be added to the structure when storing the string so no need to add it at compiling time.
    \ note not all letters can be used to make a system semaphore!
    \ See above word for how the structure is organized because this word does the same thing!
    nextname CREATE sem-info-sem_t dup 2, latest name>string dup dup 1+ here dup rot allot rot erase swap cmove  ;

: sema-mk-named ( nvalue asema% -- nflag ) \ nflag will be false if semaphore was made in system.  The pointer to semaphore is stored in asema% now.
    swap >r dup 2 cells + r> semaphore-constants >r swap 436 swap sem-open dup r> = -rot swap ! ;

: sema-op-exist ( asem% -- nflag ) \ nflag will be false if existing semaphore was opened.  The pointer to semaphore is stored in asema% now.
    dup 2 cells + 0 sem-openexisting dup semaphore-constants swap drop = -rot swap ! ;       

: sema@ ( asema% -- nvalue nflag ) \ nflag is false if nvalue is a valid number.  nvalue is semaphore current value.  
    try
	dup cell+ dup rot @ swap sem-getvalue swap @ swap
    restore endtry ;

: sema-close { asema% -- nflag } \ nflag is false if semaphore connection to this process is closed properly.
    try
	asema% @ sem-close
    restore endtry ;

: sema+ { asema% -- nflag } \ nflag is false if semaphore was incremented properly.
    try
	asema% @ sem-inc
    restore endtry ;

: sema- { asema% -- nflag } \ nflag is false if semaphore was decrementd properly.  Note this word is blocking!!!
    try
	asema% @ sem-dec
    restore endtry ;

: sema-try- { asema% -- nflag } \ nflag is false if semaphore was decrement properly. Not this word does not block!!
    try
	asema% @ sem-trydec
    restore endtry ;
: sema-delete { asema% -- nflag } \ nflag is false if semaphore was deleted from system properly.
    try
	asema% 2 cells + sem-unlink
    restore endtry ;

 
\ ************************************************************ 

[THEN]