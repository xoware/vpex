package com.xoware.vpexconnectionmanager;

import android.content.Intent;
import android.os.Bundle;
import android.support.v4.app.FragmentActivity;
import android.support.v4.app.NavUtils;
import android.util.Log;
import android.view.MenuItem;

public class ConnectionDetailActivity extends FragmentActivity implements DeleteConfirmationDialogFragment.DeleteConfirmationCallback, ConnectionDetailFragment.NFCCallback {

	public static final String TAG = "VPExConnectionManager";
	
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_connection_detail);

        getActionBar().setDisplayHomeAsUpEnabled(true);

        if (savedInstanceState == null) {
            Bundle arguments = new Bundle();
            arguments.putString(ConnectionDetailFragment.ARG_ITEM_ID,
                    getIntent().getStringExtra(ConnectionDetailFragment.ARG_ITEM_ID));
            ConnectionDetailFragment fragment = new ConnectionDetailFragment();
            fragment.setArguments(arguments);
            getFragmentManager().beginTransaction()
                    .add(R.id.connection_detail_container, fragment)
                    .commit();
        }
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == android.R.id.home) {
            NavUtils.navigateUpTo(this, new Intent(this, ConnectionListActivity.class));
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    @Override
    public void confirmDeletionOfConnection(String connection, boolean confirmed) {
		VPExDirectory vdirectory = VPExDirectory.getInstance();
    	if (confirmed)  {
    		Log.d(TAG, "(DetailActivity) delete of " + connection + " confirmed!");
    		// nuke it
    		vdirectory.delete(connection);
        	NavUtils.navigateUpTo(this, new Intent(this, ConnectionListActivity.class));
    	} else {
    		Log.d(TAG, "don't do it!  don't delete " + connection + "!");
    		getFragmentManager().popBackStackImmediate();
    	}
    }
    
    public void writeNFCTagForString(String s, String pw) {
    	Log.d(TAG, "asked to write nfc tag for " + s);
    	if (pw != null)  {
    		Log.d(TAG, "password supplied = " + pw);
    	} else {
    		Log.d(TAG, "no password");
    	}
    	Intent nfcIntent = new Intent(this, NFCWriteActivity.class);
    	nfcIntent.putExtra("com.xoware.vpexconnectionmanager.data", s);
    	if (pw != null)  {
    		nfcIntent.putExtra("com.xoware.vpexconnectionmanager.password",  pw);
    	}
		startActivity(nfcIntent);
    }
}
