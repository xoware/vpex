#!/bin/bash

BLD_DIR=${HOME}/Library/Developer/Xcode/DerivedData/ExoKey-gsgnjlhqhdmxrgewgrfinfatxfdc/Build/Products/Debug

OUT_DIR=/tmp/xokey_image

rm -rf $OUT_DIR
mkdir -p $OUT_DIR
cp -av ${BLD_DIR}/XOkey.app $OUT_DIR
hdiutil create -volname "x.o.ware XOkey" -srcfolder $OUT_DIR -ov -format UDZO XOkey.dmg

# make bacground image like described at:
# http://stackoverflow.com/questions/96882/how-do-i-create-a-nice-looking-dmg-for-mac-os-x-using-command-line-tools
