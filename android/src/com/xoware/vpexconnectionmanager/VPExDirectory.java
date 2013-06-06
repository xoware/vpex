package com.xoware.vpexconnectionmanager;

import java.io.File;
import java.io.FileFilter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;

import android.os.Environment;
import android.util.Log;

public final class VPExDirectory {

	private static final String TAG = "VPExConnectionManager";
	private static VPExDirectory instance = null;

	private List<String> vpexConnections;
	public boolean isEmpty = false;
	
	private VPExDirectory() {
		// Exists only to defeat instantiation.
	}

	public static VPExDirectory getInstance() {
		if (instance == null) {
			instance = new VPExDirectory();
		}
		return instance;
	}

	public List<String> directory()
	{
		if (vpexConnections == null)  {
			vpexConnections = new ArrayList<String>();
		}
		return vpexConnections;
	}
	
    public void update()  {
    	List<String> connections = this.directory();
    	connections.clear();
    	Log.d(TAG, "beginning populate");
    	// only attempt this if external storage is available
    	if (Environment.getExternalStorageState().equals(Environment.MEDIA_MOUNTED))  {
    		File storageDir = MyApp.getContext().getExternalFilesDir(null);
        	String[] fileList = storageDir.list();
	    	Log.d(TAG, "number of files = " + fileList.length);
	    	if (fileList.length == 0)  {
	    		connections.add("-- NONE --");
	    		isEmpty = true;
	    	} else {
		    	for (int i = 0; i < fileList.length; i++)  {
		    		Log.d(TAG, "found file: " + fileList[i]);
		    		if (fileList[i].endsWith(".conf"))  {
		    			Log.d(TAG, "FOUND VPEX CONFIG");
		    			int dex = fileList[i].indexOf(".conf");
		    			String connectionName = fileList[i].substring(0, dex);
		    			Log.d(TAG, "connection name = " + connectionName);
		    			connections.add(connectionName);
		    		}
		    	}
	    		isEmpty = false;
		    	Collections.sort(connections);
	    	}
    	} else {
    		connections.add("-- NONE --");
    		isEmpty = true;
    	}
    	// notify data set changed
    }

    public void delete(String connection)  {
    	final String conn = connection;

    	if (Environment.getExternalStorageState().equals(Environment.MEDIA_MOUNTED))  {
			File directory = MyApp.getContext().getExternalFilesDir(null);
	
			File[] toBeDeleted = directory.listFiles(new FileFilter() {
				public boolean accept(File theFile) {
					if (theFile.isFile()) {
						return theFile.getName().startsWith(conn);
					}
					return false;
				}
			});
	
			Log.d(TAG, "ABOUT TO DELETE: " + Arrays.toString(toBeDeleted));
			for (File deletableFile : toBeDeleted) {
				deletableFile.delete();
			}
    	} else {
    		Log.d(TAG, "unable to delete");
    	}
		this.update();
    }

}
