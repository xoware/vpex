package com.xoware.vpexconnectionmanager;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.net.URL;
import java.net.URLConnection;
import java.net.URLDecoder;

import android.media.MediaPlayer;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager.NameNotFoundException;
import android.graphics.Color;
import android.graphics.LightingColorFilter;
import android.media.MediaPlayer.OnCompletionListener;
import android.net.Uri;
import android.os.AsyncTask;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.app.Activity;
import android.app.Dialog;
import android.app.ProgressDialog;
import android.text.format.Time;
import android.util.Log;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ScrollView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

//import android.graphics.PorterDuff;

public class MainActivity extends Activity implements OnClickListener {

	private static final String TAG = "VPExConnectionManager";
	Button connectButton;
	Spinner vpexConnectionList;
	boolean connected = false;
	TextView connectionStatusIndicator;
	EditText logWindow;
	ScrollView logScroller;
	ArrayAdapter<String> vpexConnectionListAdapter;
	VPExDirectory directory;
	public static final int DIALOG_DOWNLOAD_PROGRESS = 0;
	private ProgressDialog mProgressDialog;

	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		directory = VPExDirectory.getInstance();

		// set up default prefs
		PreferenceManager.setDefaultValues(this, R.xml.prefs, false);

		setContentView(R.layout.activity_main);

		logWindow = (EditText) findViewById(R.id.logWindow);
		logScroller = (ScrollView) findViewById(R.id.logScroller);

		connectButton = (Button) findViewById(R.id.connectButton);
		connectButton.setOnClickListener(this);
		// connectButton.getBackground().setColorFilter(0xFF00FF00,
		// PorterDuff.Mode.MULTIPLY);
		connectButton.getBackground().setColorFilter(
				new LightingColorFilter(0xFFFFFFFF, 0xFF00FF00)); // AA vs FF

		connectionStatusIndicator = (TextView) findViewById(R.id.statusIndicator);

		vpexConnectionList = (Spinner) findViewById(R.id.vpexConnectionList);
		vpexConnectionListAdapter = new ArrayAdapter<String>(this,
				android.R.layout.simple_spinner_item, directory.directory());
		vpexConnectionListAdapter
				.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
		vpexConnectionList.setAdapter(vpexConnectionListAdapter);
		vpexConnectionListAdapter.notifyDataSetChanged();

		Uri launchIntent = getIntent().getData();

		if (launchIntent != null) {
			Log.d(TAG, "launched with intent " + launchIntent);
			HandleLaunchOptions(launchIntent);
		} else {
			Log.d(TAG, "no launch options");
		}
	}

	@Override
	protected void onNewIntent (Intent intent)
	{
		Log.d(TAG, "NEW INTENT RECEIVED");
		Uri launchIntent = intent.getData();

		if (launchIntent != null) {
			Log.d(TAG, "it is " + launchIntent);
			HandleLaunchOptions(launchIntent);
		} else {
			Log.d(TAG, "huh? got nothing? how did this happen?!");
		}
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		MenuInflater inflater = getMenuInflater();
		inflater.inflate(R.menu.menu, menu);
		return true;
	}

	@Override
	protected void onResume() {
		super.onResume();
		populateConnectionList();
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		switch (item.getItemId()) {
		case R.id.itemRefresh:
			populateConnectionList();
			Toast.makeText(this, "Refreshed VPEx connection list.",
					Toast.LENGTH_SHORT).show();
			break;
		case R.id.itemManage:
			startActivity(new Intent(this, ConnectionListActivity.class));
			break;
		case R.id.itemPrefs:
			startActivity(new Intent(this, PrefsActivity.class));
			break;
		case R.id.itemAbout:
			AboutDialog about = new AboutDialog(this);
			String version = "";
			int versionCode = 0;
			String versionString = "About This App";
			// String versionString = "About " + getResources().getText(R.string.app_name).toString();
			/* try {
				PackageInfo manager = getPackageManager().getPackageInfo(
						getPackageName(), 0);
				version = manager.versionName;
				versionCode = manager.versionCode;
				versionString += " v" + version + " build " + versionCode;
			} catch (NameNotFoundException e) {
				// Handle exception
				Log.d(TAG, "could not get version info");
			} */
			about.setTitle(versionString);
			about.show();
			break;
		}
		return true;
	}

	public void onClick(View v) {
		String connectionName = vpexConnectionList.getSelectedItem().toString();
		SharedPreferences prefs = PreferenceManager
				.getDefaultSharedPreferences(this);
		boolean playSounds = prefs.getBoolean("enableSounds", true);
		if (!connected) {
			if (playSounds) {
				MediaPlayer mp = MediaPlayer.create(this, R.raw.connect_sound);
				mp.start();
				mp.setOnCompletionListener(new OnCompletionListener() {
					@Override
					public void onCompletion(MediaPlayer mp) {
						mp.release();
					}
				});
			}
			connectButton.setText(getString(R.string.disconnect_button));
			// connectButton.getBackground().setColorFilter(0xFFFF0000,
			// PorterDuff.Mode.MULTIPLY);
			connectButton.getBackground().setColorFilter(
					new LightingColorFilter(0xFFFFFFFF, 0xFFFF0000)); // AA vs
																		// FF
			connectionStatusIndicator
					.setText(getString(R.string.text_status_up));
			connectionStatusIndicator.setTextColor(Color.parseColor("#007F00"));
			Log.d(TAG, "onClicked, start connection to " + connectionName);
			addLogMessage("starting connection to " + connectionName);
			vpexConnectionList.setEnabled(false);
			connected = true;
		} else {
			if (playSounds) {
				MediaPlayer mp = MediaPlayer.create(this,
						R.raw.disconnect_sound);
				mp.start();
				mp.setOnCompletionListener(new OnCompletionListener() {
					@Override
					public void onCompletion(MediaPlayer mp) {
						mp.release();
					}
				});
			}
			connectButton.setText(getString(R.string.connect_button));
			// connectButton.getBackground().setColorFilter(0xFF00FF00,
			// PorterDuff.Mode.MULTIPLY);
			connectButton.getBackground().setColorFilter(
					new LightingColorFilter(0xFFFFFFFF, 0xFF00FF00)); // AA vs
																		// FF
			connectionStatusIndicator
					.setText(getString(R.string.text_status_down));
			connectionStatusIndicator.setTextColor(Color.parseColor("#FF0000"));
			Log.d(TAG, "onClicked, stop connection to " + connectionName);
			addLogMessage("connection to " + connectionName + " terminated");
			vpexConnectionList.setEnabled(true);
			connected = false;
		}
	}

	public void populateConnectionList() {
		directory.update();
		if (directory.isEmpty) {
			vpexConnectionList.setEnabled(false);
			connectButton.setEnabled(false);
		} else {
			vpexConnectionList.setEnabled(true);
			connectButton.setEnabled(true);
		}
		vpexConnectionListAdapter.notifyDataSetChanged();
	}

	public void addLogMessage(String s) {
		Time today = new Time(Time.getCurrentTimezone());
		today.setToNow();

		String currentText = logWindow.getText().toString();
		String newLogMsg = today.format("%Y-%m-%d %H:%M:%S") + ": " + s + "\n";
		logWindow.setText(currentText + newLogMsg);
		logScroller.post(new Runnable() {
			public void run() {
				logScroller.smoothScrollTo(0, logWindow.getBottom());
			}
		});
	}

	public void HandleLaunchOptions(Uri options) {
		String scheme = options.getScheme();
		if (scheme.equals("vpex")) {
			Log.d(TAG, "launched with vpex url");
			// String decodedName = Html.fromHtml(options.getHost()).toString();
			String decodedName;
			String password = null, decryptedPassword = null;
			try {
				decodedName = URLDecoder.decode(options.getHost(), "UTF-8");
				Log.d(TAG, "launch vpex connection " + decodedName);
				password = options.getQueryParameter("pw");
				if (password != null)  {
					Log.d(TAG, "got a password, encrypted = " + password);
			    	SimpleCrypto crypto = new SimpleCrypto();
					String masterPw = IDManager.getInstance().id(MyApp.getContext());
			        try {
			        	decryptedPassword = crypto.decrypt(masterPw, password);
						Log.d(TAG, "decrypted = " + decryptedPassword);
					} catch (Exception e) {
						// TODO Auto-generated catch block
						e.printStackTrace();
					}
				}
				if (decryptedPassword != null)  {
					addLogMessage("NFC tag has requested us to initiate a connection to "
						+ decodedName + " with password " + decryptedPassword);
				} else {
					addLogMessage("NFC tag has requested us to initiate a connection to "
							+ decodedName);
				}
			} catch (UnsupportedEncodingException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
		} else if (scheme.equals("http") || scheme.equals("https")) {
			Log.d(TAG, "must download via http(s)");
			new DownloadFileAsync().execute(options.toString());
		} else if (scheme.equals("file")) {
			Log.d(TAG, "local file");
			String fileLocation = options.getEncodedPath();
			//String configNameTemp = options.getLastPathSegment();
			//int dex = configNameTemp.indexOf(".vpx");
			//String configName = configNameTemp.substring(0, dex);
			new UnzipFileAsync().execute(fileLocation);
			//UnarchiveVpexConfigurationFile(configName, fileLocation);
		} else if (scheme.equals("content")) {
			Log.d(TAG, "content provider");
			new ContentProviderFileAsync().execute(options.toString());
		} else {
			Log.d(TAG, "received unknown intent type " + scheme);
		}
	}

	public void UnarchiveVpexConfigurationFile(String configName,
			String location) {
		String unzipLocation = getExternalFilesDir(null).getAbsolutePath();
		Log.d(TAG, "UNZIP " + location + " to " + unzipLocation);
		Decompress d = new Decompress(location, unzipLocation);
		d.unzip();
		// delete it
		File nukeMe = new File(location);
		// XXX Temp debug code to examine the file
		/*
		StringBuilder text = new StringBuilder();
		try {
			BufferedReader br = new BufferedReader(new FileReader(nukeMe));
			String line;

			while ((line = br.readLine()) != null) {
				text.append(line);
				text.append('\n');
			}
			Log.d(TAG, "CONTENTS OF TEMP FILE = " + text);
			br.close();
		}
		catch (IOException e) {
			// You'll need to add proper error handling here
		} */
		// XXX end of temp debug code
		if (nukeMe.delete()) {
			Log.d(TAG, "Delete successful");
		} else {
			Log.d(TAG, "Delete UNSUCCESSFUL");
		}
		Log.d(TAG, "populating conn list");
//		populateConnectionList();
		/*
		Toast.makeText(this,
				"Installed new VPEx configuration '" + configName + "'.",
				Toast.LENGTH_SHORT).show();
		*/
	}

	@Override
	protected Dialog onCreateDialog(int id) {
		switch (id) {
		case DIALOG_DOWNLOAD_PROGRESS:
			mProgressDialog = new ProgressDialog(this);
			mProgressDialog.setMessage("Downloading file..");
			mProgressDialog.setProgressStyle(ProgressDialog.STYLE_HORIZONTAL);
			mProgressDialog.setCancelable(false);
			mProgressDialog.show();
			return mProgressDialog;
		default:
			return null;
		}
	}

	class ContentProviderFileAsync extends AsyncTask<String, String, String> {
		String tempFile;
		
		@Override
		protected String doInBackground(String... file) {
			String cpString = file[0];
			Log.d(TAG, "ContentProviderFileAsync invoked with " + cpString);
			Uri cp = Uri.parse(cpString);
			try {
				InputStream is = getContentResolver().openInputStream(cp);
				File outputDir = getCacheDir(); // context being the Activity
												// pointer
				File outputFile = File
						.createTempFile("tmp", ".vpx", outputDir);
				tempFile = outputFile.getName();
				Log.d(TAG, "temp file = " + tempFile);
				String outputFilePath = outputFile.getAbsolutePath();
				Log.d(TAG, "attempting to save attachment to " + outputFilePath);
				OutputStream out = new FileOutputStream(outputFile);
				int read = 0;
				byte[] bytes = new byte[1024];
				while ((read = is.read(bytes)) != -1) {
					out.write(bytes, 0, read);
				}
				is.close();
				out.flush();
				out.close();
			} catch (FileNotFoundException e) {
				Log.d(TAG, Log.getStackTraceString(e));
				;
			} catch (IOException e) {
				Log.d(TAG, Log.getStackTraceString(e));
				;
			}
			return null;
		}
		
		@Override
		protected void onPostExecute(String unused) {
			Log.d(TAG, "onPostExecute");
			Log.d(TAG, "attempting to uncompress" + tempFile);
			new UnzipFileAsync().execute(tempFile);
		}
	}
	
	class UnzipFileAsync extends AsyncTask<String, String, String> {

		@Override
		protected String doInBackground(String... file) {
			String location = file[0];
			Log.d(TAG, "doInBackground with " + location);
			String unzipLocation = getExternalFilesDir(null).getAbsolutePath();
			Log.d(TAG, "BACKGROUND UNZIP " + location + " to " + unzipLocation);
			Decompress d = new Decompress(location, unzipLocation);
			d.unzip();
			// delete it
			File nukeMe = new File(location);
			if (nukeMe.delete()) {
				Log.d(TAG, "Delete successful");
			} else {
				Log.d(TAG, "Delete UNSUCCESSFUL");
			}
			return null;
		}

		@Override
		protected void onPostExecute(String unused) {
			Log.d(TAG, "onPostExecute");
			Toast.makeText(getBaseContext(), "Imported new VPEx configuration.", Toast.LENGTH_SHORT).show();
			populateConnectionList();
		}
	}

	class DownloadFileAsync extends AsyncTask<String, String, String> {
		String destination;

		@SuppressWarnings("deprecation")
		@Override
		protected void onPreExecute() {
			super.onPreExecute();
			showDialog(DIALOG_DOWNLOAD_PROGRESS);
			File outputDir = getCacheDir(); // context being the Activity
											// pointer
			File outputFile;
			try {
				outputFile = File.createTempFile("tmp", ".vpx", outputDir);
				destination = outputFile.getAbsolutePath();
				Log.d(TAG, "output destination = " + destination);
			} catch (IOException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
		}

		@Override
		protected String doInBackground(String... aurl) {
			int count;

			try {
				URL url = new URL(aurl[0]);
				URLConnection conexion = url.openConnection();
				conexion.connect();

				int lenghtOfFile = conexion.getContentLength();
				Log.d("ANDRO_ASYNC", "Lenght of file: " + lenghtOfFile);

				InputStream input = new BufferedInputStream(url.openStream());
				OutputStream output = new FileOutputStream(destination);

				byte data[] = new byte[1024];

				long total = 0;

				while ((count = input.read(data)) != -1) {
					total += count;
					publishProgress("" + (int) ((total * 100) / lenghtOfFile));
					output.write(data, 0, count);
				}

				output.flush();
				output.close();
				input.close();
			} catch (Exception e) {
			}
			return null;

		}

		protected void onProgressUpdate(String... progress) {
			Log.d("ANDRO_ASYNC", progress[0]);
			mProgressDialog.setProgress(Integer.parseInt(progress[0]));
		}

		@SuppressWarnings("deprecation")
		@Override
		protected void onPostExecute(String unused) {
			dismissDialog(DIALOG_DOWNLOAD_PROGRESS);
			new UnzipFileAsync().execute(destination);
		}
	}

}
