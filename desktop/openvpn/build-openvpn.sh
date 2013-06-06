#!/bin/bash
# OpenVPN Client + Installer Build Script
#
# This script will build OpenVPN for windows on a cross compile Linux environment
# 
# Copyright (C) 2010
# by Quintin Beukes <quintin _AT_ last _DOT_ za _DOT_ net>
# http://qbeukes.blogspot.com
#
# Licensed under the BSD License

function info()
{
  echo -ne "\n\n$@\n\n"
}

function fail()
{
  echo -ne "\n\n@\n\n" >&2
  exit 1
}

function checkDir()
{
  [ -d "$1" ] && return 0
  fail "$2 directory doesn't exist!"
}

function findDir()
{
  local parent="$1"
  local prefix="$2"

  local result=$(find "$parent" -maxdepth 1 -name "$prefix*" -type d | sort -rn | head -n1)

  if [ "x$result" = "x" ]
  then
    fail "Cannot find prebuilt package directory '$prefix*' in '$parent'"
  fi

  echo $result
}

dobuildopenvpn=1

SCRIPT_DIR=$(cd `dirname $0`; pwd)
source "$SCRIPT_DIR/env.sh"

ARCHIVE_DIR="$SCRIPT_DIR/archive"

# will be removed
BUILD_DIR="$SCRIPT_DIR/src"

if [ $dobuildopenvpn -eq 1 ]
then
  checkDir "$ARCHIVE_DIR" "Archive"

  PREBUILT="$ARCHIVE_DIR/"*-prebuilt.tbz
  OPENVPN="$ARCHIVE_DIR/openvpn"-*.tar.gz

  info "Removing old build directory."
  rm -rf "$BUILD_DIR"
  mkdir "$BUILD_DIR"

  source "$SCRIPT_DIR"/env.sh

  cd src/

  info "Extracting prebuilt libraries"
  # extract prebuilt
  tar -jxpf $PREBUILT
  mv *prebuilt*/* ./
  rm -rf gen-isntall

  info "Extracting OpenVPN source."
  # extract openvpn
  tar -zxpf $OPENVPN

  # Find the prebuilt package directories
  #export LZODIR=$(findDir "$BUILD_DIR" "lzo-")
  export LZODIR=../lzo-2.02
  export LZO_DIR=$LZODIR
  #export OPENSSLDIR=$(findDir "$BUILD_DIR" "openssl-")
  export OPENSSLDIR=../openssl-0.9.8l
  export OPENSSL_DIR=$OPENSSLDIR
  #export PKCS11DIR=$(findDir "$BUILD_DIR" "pkcs11-helper")
  export PKCS11DIR=../pkcs11-helper
  export PKCS11_DIR=$PKCS11DIR
  export PKCS11_HELPER_DIR=$PKCS11DIR

  echo "LZODIR is $LZODIR"
  echo "LZO_DIR is $LZO_DIR"
  echo "OPENSSLDIR is $OPENSSLDIR"
  echo "OPENSSL_DIR is $OPENSSL_DIR"
  echo "PKCS11DIR is $PKCS11DIR"
  echo "PKCS11_DIR is $PKCS11_DIR"
  echo "PKCS11_HELPER_DIR is $PKCS11_HELPER_DIR"

  # build openvpn
  cd "$BUILD_DIR"/openvpn-*

  if [ "x$MAKENSIS" != "x" ]
  then
    info "Building OpenVPN with installer"
  else
    info "Building OpenVPN without installer"
    MAKENSIS="echo"
  fi
    
  # prepare the build
  sed -i -e "s#^'/c/Program Files/NSIS/makensis'#'$MAKENSIS'#" install-win32/buildinstaller
  sed -i -e "s#./configure #./configure --build=$TARGET --includedir=$MINGDIR/include --libdir=$MINGDIR/lib --enable-password-save #" "install-win32/makeopenvpn"

  # since we don't have DDK set up, just copy the prebuilt TAP driver
  > install-win32/maketap
  cat > install-win32/maketapinstall <<EOF
  source autodefs/defs.sh
  cp -Rp "$BUILD_DIR/gen-prebuilt/tapinstall" "\$GENOUT"/
  cp -Rp "$BUILD_DIR/gen-prebuilt/driver" "\$GENOUT"/
  echo "Copied prebuilt tap drivers."
EOF
    
  # start build
  chmod a+x domake-win install-win32/*
  OPENSSL_DIR=$OPENSSL_DIR \
  LZO_DIR=$LZO_DIR \
  PKCS11_HELPER_DIR=$PKCS11_HELPER_DIR \
  ./domake-win \
    && info "Successfully compiled OpenVPN." \
    || fail "Failed to compile OpenVPN."

  source autodefs/defs.sh
  [ "x$GENOUT" = "x" ] && fail "GENOUT variable not defined."

  # copy the installer and results to the prefix
  mkdir -p "$PREFIX"
  cp -Rp "$GENOUT"/* "$PREFIX"/
fi

