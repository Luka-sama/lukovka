<?php
$host = 'localhost';
$db = 'lukovka';
$user = 'lukovka';
$password = 'RDmNOf9SsFFliWaUBDEJ';

$filePath = '/srv/lukovka/tasks.txt';

$tableName = 'tasks';

function camelToSnakeCase($input)
{
    return strtolower(preg_replace('/([a-z])([A-Z])/', '$1_$2', $input));
}

try {
    $dsn = "pgsql:host=$host;dbname=$db";
    $pdo = new PDO($dsn, $user, $password, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
    ]);

    $fileHandle = fopen($filePath, 'r');
    if (!$fileHandle) {
        throw new Exception("Unable to open file: $filePath");
    }

    $rows = [];

    while (($line = fgets($fileHandle)) !== false) {
        $json = json_decode(trim($line), true);
        if (!$json) {
            throw new Exception("Invalid JSON in file: $line");
        }

        $snakeCaseJson = [];
        foreach ($json as $key => $value) {
            $snakeCaseKey = camelToSnakeCase($key);

            if (is_array($value)) {
                $value = array_map(fn($v) => '"' . str_replace('"', '\"', $v) . '"' , $value);
                $snakeCaseJson[$snakeCaseKey] = '{' . implode(',', $value) . '}';
			} elseif (is_bool($value)) {
				$snakeCaseJson[$snakeCaseKey] = $value ? 'true' : 'false';
			} else {
                $snakeCaseJson[$snakeCaseKey] = $value;
            }
        }
        $rows[] = $snakeCaseJson;
    }

    fclose($fileHandle);

    foreach ($rows as $index => $row) {
        $columns = [];
        $placeholders = [];
        $values = [];

        foreach ($row as $key => $value) {
            if ($value !== null) {
                $columns[] = "\"$key\"";
                $placeholderKey = $key . '_' . $index;
                $placeholders[] = ":$placeholderKey";
                $values[$placeholderKey] = $value;
            }
        }

        $columnsList = implode(', ', $columns);
        $placeholdersList = implode(', ', $placeholders);

        $sql = "INSERT INTO $tableName ($columnsList) VALUES ($placeholdersList)";
        $stmt = $pdo->prepare($sql);
        $stmt->execute($values);
    }

    echo "Data successfully inserted into $tableName!" . PHP_EOL;
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . PHP_EOL;
}
