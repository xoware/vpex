package com.xoware.vpexconnectionmanager;

import android.content.Intent;
import android.os.Bundle;
import android.support.v4.app.FragmentActivity;
import android.support.v4.app.NavUtils;
import android.util.Log;
import android.view.MenuItem;

public class ConnectionListActivity extends FragmentActivity implements
		ConnectionListFragment.Callbacks, ConnectionDetailFragment.NFCCallback,
		DeleteConfirmationDialogFragment.DeleteConfirmationCallback {

	public static final String TAG = "VPExConnectionManager";

	private VPExDirectory directory = VPExDirectory.getInstance();
	private boolean mTwoPane;

	ConnectionDetailFragment foo;

	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_connection_list);
		getActionBar().setDisplayHomeAsUpEnabled(true);

		if (findViewById(R.id.connection_detail_container) != null) {
			mTwoPane = true;
			((ConnectionListFragment) getSupportFragmentManager()
					.findFragmentById(R.id.connection_list))
					.setActivateOnItemClick(true);
		}
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		switch (item.getItemId()) {
		case android.R.id.home:
			NavUtils.navigateUpFromSameTask(this);
			return true;
		}
		return super.onOptionsItemSelected(item);
	}

	@Override
	public void onItemSelected(String id) {
		if (mTwoPane) {
			Bundle arguments = new Bundle();
			arguments.putString(ConnectionDetailFragment.ARG_ITEM_ID, id);
			ConnectionDetailFragment fragment = new ConnectionDetailFragment();
			foo = fragment;
			fragment.setArguments(arguments);
			getFragmentManager().beginTransaction()
					.replace(R.id.connection_detail_container, fragment)
					.commit();
		} else {
			Intent detailIntent = new Intent(this,
					ConnectionDetailActivity.class);
			detailIntent.putExtra(ConnectionDetailFragment.ARG_ITEM_ID, id);
			startActivity(detailIntent);
		}
	}

	public void onWindowFocusChanged(boolean hasFocus) {
		Log.d(TAG, "BLAH BLAH BLAH");
		if (directory.isEmpty)  {
			ConnectionListFragment fragment = (ConnectionListFragment) getSupportFragmentManager()
					.findFragmentById(R.id.connection_list);
			if (fragment != null) // could be null if not instantiated yet
			{
				if (fragment.getListView() != null) {
					// no need to call if fragment's onDestroyView()
					// has since been called.
					fragment.getListView().setEnabled(false); // do what updates are
												// required
				}
			}
		}
	}

	public void confirmDeletionOfConnection(String connection, boolean confirmed) {
		VPExDirectory vdirectory = VPExDirectory.getInstance();

		if (confirmed) {
			Log.d(TAG, "(ListActivity) delete of " + connection + " confirmed!");
			// nuke it
			vdirectory.delete(connection);
			if (!mTwoPane) {
				NavUtils.navigateUpTo(this, new Intent(this,
						ConnectionListActivity.class));
			} else {
				getFragmentManager().beginTransaction().remove(foo).commit();
				ConnectionListFragment fragment = (ConnectionListFragment) getSupportFragmentManager()
						.findFragmentById(R.id.connection_list);
				if (fragment != null) // could be null if not instantiated yet
				{
					if (fragment.getView() != null) {
						// no need to call if fragment's onDestroyView()
						// has since been called.
						fragment.updateDisplay(); // do what updates are
													// required
					}
				}
			}
		} else {
			Log.d(TAG, "don't do it! don't delete " + connection + "!");
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
