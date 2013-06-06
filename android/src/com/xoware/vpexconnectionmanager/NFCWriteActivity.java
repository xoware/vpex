package com.xoware.vpexconnectionmanager;

import java.io.UnsupportedEncodingException;
import java.net.URLEncoder;

import android.nfc.NdefMessage;
import android.nfc.NdefRecord;
import android.nfc.NfcAdapter;
import android.nfc.Tag;
import android.nfc.tech.Ndef;
import android.nfc.tech.NdefFormatable;
import android.os.AsyncTask;
import android.os.Bundle;
import android.app.Activity;
import android.app.PendingIntent;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.TextView;
import android.widget.Toast;
import android.support.v4.app.NavUtils;

public class NFCWriteActivity extends Activity implements OnClickListener {
	private static final String TAG = "VPExConnectionManager";
	TextView statusLine;
	Button cancelButton;
	CheckBox writeProtectCheckbox;
	NfcAdapter nfc;
	boolean inWriteMode;
	String urlToEncode;
	String password;

	@Override
	public void onCreate(Bundle savedInstanceState) {
		Log.d(TAG, "NFC onCreate called");
		String value = "INVALID";

		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_nfcwrite);
		setFinishOnTouchOutside(false);
		cancelButton = (Button) findViewById(R.id.cancel_button);
		cancelButton.setOnClickListener(this);
		writeProtectCheckbox = (CheckBox)findViewById(R.id.write_protect_checkbox);

		nfc = NfcAdapter.getDefaultAdapter(this);

		// getActionBar().setDisplayHomeAsUpEnabled(true);
		// Log.d(TAG, "getactionbar");
		Bundle extras = getIntent().getExtras();
		Log.d(TAG, "extras = " + extras);
		if (extras != null) {
			value = extras.getString("com.xoware.vpexconnectionmanager.data");
			Log.d(TAG, "called with " + value);
			password = extras
					.getString("com.xoware.vpexconnectionmanager.password");
			if (password == null) {
				Log.d(TAG, "no password supplied");
			} else {
				Log.d(TAG, "password = " + password);
			}
		} else {
			Log.d(TAG, "not able to get data that was passed in to me");
			Toast.makeText(this, "Could not write NFC tag.", Toast.LENGTH_SHORT)
					.show();
			finish();
		}
		statusLine = (TextView) findViewById(R.id.status_message);
		statusLine.setText("waiting for NFC tag...");
		String encodedValue;
		final String fullURL;
		try {
			encodedValue = URLEncoder.encode(value, "UTF-8");
			if (password != null) {
				fullURL = "vpex://" + encodedValue + "?pw=" + password;
			} else {
				fullURL = "vpex://" + encodedValue;
			}
			Log.d(TAG, "FULL URL = " + fullURL);
			urlToEncode = fullURL;
		} catch (UnsupportedEncodingException e) {
			// TODO Auto-generated catch block
			Toast.makeText(this, "Could not write NFC tag.", Toast.LENGTH_SHORT)
					.show();
			finish();
			e.printStackTrace();
		}
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		getMenuInflater().inflate(R.menu.activity_nfcwrite, menu);
		return true;
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
	public void onResume() {
		super.onResume();
		Log.d(TAG, "NFC onResume called");

		if (!inWriteMode) {
			Log.d(TAG, "NOT IN WRITE MODE");
			IntentFilter discovery = new IntentFilter(
					NfcAdapter.ACTION_TAG_DISCOVERED);
			IntentFilter[] tagFilters = new IntentFilter[] { discovery };
			Intent i = new Intent(this, getClass())
					.addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP
							| Intent.FLAG_ACTIVITY_CLEAR_TOP);
			Log.d(TAG,
					"inserting extras: "
							+ getIntent().getExtras().getString(
									"com.xoware.vpexconnectionmanager.data"));
			i.putExtra(
					"com.xoware.vpexconnectionmanager.data",
					getIntent().getExtras().getString(
							"com.xoware.vpexconnectionmanager.data"));
			if (getIntent().getExtras().getString(
					"com.xoware.vpexconnectionmanager.password") != null) {
				Log.d(TAG, "storing password");
				i.putExtra(
						"com.xoware.vpexconnectionmanager.password",
						getIntent().getExtras().getString(
								"com.xoware.vpexconnectionmanager.password"));
			} else {
				Log.d(TAG, "no password to store");
			}

			PendingIntent pi = PendingIntent.getActivity(this, 0, i, 0);

			inWriteMode = true;
			nfc.enableForegroundDispatch(this, pi, tagFilters, null);
		} else {
			Log.d(TAG, "IN WRITE MODE");
		}
	}

	@Override
	public void onPause() {
		Log.d(TAG, "NFC onPause called");
		if (isFinishing()) {
			Log.d(TAG, "disableForegroundDispatch");
			nfc.disableForegroundDispatch(this);
			inWriteMode = false;
		} else {
			Log.d(TAG, "NOT isFinishing");
		}

		super.onPause();
	}

	@Override
	protected void onNewIntent(Intent intent) {
		boolean writeProtectTag = false;
		Log.d(TAG, "NFC onNewIntent called");
		if (inWriteMode
				&& NfcAdapter.ACTION_TAG_DISCOVERED.equals(intent.getAction())) {
			cancelButton.setEnabled(false);
			writeProtectCheckbox.setEnabled(false);
			if (writeProtectCheckbox.isChecked())  {
				Log.d(TAG, "WRITE PROTECT REQUESTED");
				writeProtectTag = true;
			}
			statusLine.setText("writing tag...");

			Tag tag = intent.getParcelableExtra(NfcAdapter.EXTRA_TAG);
			Log.d(TAG,
					"buildurlbytes "
							+ getIntent().getStringExtra(Intent.EXTRA_TEXT));
			Log.d(TAG,
					"or maybe "
							+ getIntent().getStringExtra(
									"com.xoware.vpexconnectionmanager.data")
							+ " or " + urlToEncode);
			byte[] url = buildUrlBytes(urlToEncode);
			NdefRecord record = new NdefRecord(NdefRecord.TNF_WELL_KNOWN,
					NdefRecord.RTD_URI, new byte[] {}, url);
			// XXX change this to the real app package name once we're in market
			NdefMessage msg = new NdefMessage(
					new NdefRecord[] {
							record,
							NdefRecord
									.createApplicationRecord("com.xoware.vpexconnectionmanager") });

			new WriteTask(this, msg, tag, writeProtectTag).execute();
		}
	}

	private byte[] buildUrlBytes(String url) {
		byte prefixByte = 0;
		String subset = url;

		final byte[] subsetBytes = subset.getBytes();
		final byte[] result = new byte[subsetBytes.length + 1];

		result[0] = prefixByte;
		System.arraycopy(subsetBytes, 0, result, 1, subsetBytes.length);

		return (result);
	}

	static class WriteTask extends AsyncTask<Void, Void, Void> {
		Activity host = null;
		NdefMessage msg = null;
		Tag tag = null;
		String text = null;
		boolean writeProtectTag = false;

		WriteTask(Activity host, NdefMessage msg, Tag tag, boolean writeProtectTag) {
			this.host = host;
			this.msg = msg;
			this.tag = tag;
			this.writeProtectTag = writeProtectTag;
			if (writeProtectTag)  {
				Log.d(TAG, "in constructor: WRITE PROTECT");
			} else {
				Log.d(TAG, "in constructor: WRITE ENABLE");
			}
		}

		@Override
		protected Void doInBackground(Void... arg0) {
			int size = msg.toByteArray().length;

			try {
				Ndef ndef = Ndef.get(tag);

				if (ndef == null) {
					NdefFormatable formatable = NdefFormatable.get(tag);

					if (formatable != null) {
						try {
							formatable.connect();

							try {
								if(writeProtectTag)  {
									Log.d(TAG, "WRITE PROTECTING TAG");
									formatable.formatReadOnly(msg);
								} else {
									Log.d(TAG, "WRITE ENABLING TAG");
									formatable.format(msg);
								}
							} catch (Exception e) {
								text = "Tag refused to format";
							}
						} catch (Exception e) {
							text = "Tag refused to connect";
						} finally {
							formatable.close();
						}
					} else {
						text = "Tag does not support NDEF";
					}
				} else {
					ndef.connect();

					try {
						if (!ndef.isWritable()) {
							text = "Tag is read-only";
						} else if (ndef.getMaxSize() < size) {
							text = "Message is too big for tag";
						} else if (writeProtectTag && !ndef.canMakeReadOnly())  {
							text = "This type of tag cannot be write protected.";
						} else {
							ndef.writeNdefMessage(msg);
							if (writeProtectTag && ndef.canMakeReadOnly())  {
								Log.d(TAG, "WRITE PROTECTING TAG");
								ndef.makeReadOnly();
							}
						}
					} catch (Exception e) {
						text = "Tag refused to connect";
					} finally {
						ndef.close();
					}
				}
			} catch (Exception e) {
				Log.e(TAG, "Exception when writing tag", e);
				text = "General exception: " + e.getMessage();
			}

			return (null);
		}

		@Override
		protected void onPostExecute(Void unused) {
			if (text != null) {
				Toast.makeText(host, "Unable to write tag: " + text, Toast.LENGTH_SHORT).show();
			} else {
				Toast.makeText(host, "Tag written successfully.",
						Toast.LENGTH_SHORT).show();
			}

			host.finish();
		}
	}

	public void onClick(View v) {
		Log.d(TAG, "FINISH");
		finish();
	}
}
