<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);
define('ACCESS_KEY', 't8B_JR_c_u3y0t6HSabj');
define('DATA_PATH', '/srv/lukovka.txt');
define('STATES_PATH', '/srv/lukovka_states.txt');

function stop($error) {
	http_response_code(501);
	exit($error);
}
function shutdown() {
	global $lockFile, $lockPath;
	if (!empty($lockFile)) {
		flock($lockFile, LOCK_UN);
		fclose($lockFile);
	}
	if (!empty($lockPath)) {
		unlink($lockPath);
	}
}
register_shutdown_function('shutdown');

(isset($_GET['key']) && $_GET['key'] == ACCESS_KEY) or stop("wrong key");
$path = (isset($_GET['states']) ? STATES_PATH : DATA_PATH);
$lockPath = "$path.lock";
$lockFile = fopen($lockPath, 'c+');
($lockFile) or stop("couldn't create .lock-file");
if (!flock($lockFile, LOCK_EX)) {
	fclose($lockFile);
	stop("couldn't lock .lock-file");
}

$content = file_get_contents($path);
$method = $_SERVER['REQUEST_METHOD'];
if ($method == 'GET') {
	echo $content;
} elseif (in_array($method, ['PUT', 'POST', 'DELETE'])) {
	$tasks = (!empty($content) ? explode("\n", $content) : []);
	$inputs = explode("\n", file_get_contents('php://input'));
	foreach ($inputs as $input) {
		$found = false;
		if (isset($_GET['states'])) {
			$toFind = json_encode($method == 'DELETE' ? $input : json_decode($input)->Name, JSON_UNESCAPED_UNICODE);
			$shouldStartWith = "{\"Name\":$toFind,";
		} else {
			$toFind = ($method == 'DELETE' ? $input : json_decode($input)->Id);
			$shouldStartWith = "{\"Id\":$toFind,";
		}
		for ($i = 0; $i < count($tasks); $i++) {
			if (str_starts_with($tasks[$i], $shouldStartWith)) {
				if ($method == 'PUT') {
					stop("The task $toFind already exists.");
				} elseif ($method == 'POST') {
					$tasks[$i] = $input;
				} elseif ($method == 'DELETE') {
					array_splice($tasks, $i, 1);
				}
				$found = true;
				break;
			}
		}
		if ($method == 'PUT') {
			$tasks[] = $input;
		} elseif (!$found) {
			stop("Couldn't find $toFind.");
		}
	}
	file_put_contents($path, join("\n", $tasks));
}