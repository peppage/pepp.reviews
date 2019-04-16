#!/bin/sh
USER=mca
HOST=45.55.174.193
DIR=reviews

hugo && rsync -avz --delete public/ ${USER}@${HOST}:~/${DIR}

exit 0
