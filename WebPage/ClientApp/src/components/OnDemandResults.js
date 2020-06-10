import React, { Component } from 'react';
import { Link } from 'react-router-dom';

export class OnDemandResults extends Component {
    static displayName = OnDemandResults.name;

    constructor(props) {
        super(props);
        var fileID = 0;
        var showResults = false;
        var str = props.location.pathname;
        var n = str.lastIndexOf('/');
        var result = str.substring(n + 1);
        if (!isNaN(result)) {
            fileID = parseInt(result);
            showResults = true;
        }
        


        this.state = {
            results: [],
            loading_files: false,
            loading_results: false,
            filters: true,
            fileID: fileID, //props.location.aboutProps != undefined ? props.location.aboutProps.file_id : 0,
            fileDTO: [],
            files: [],
            showResults: showResults,
            filePath: "",
            user: ""
        };


        this.getResults = this.getResults.bind(this);
        this.changeFileID = this.changeFileID.bind(this);
        this.changeFilters = this.changeFilters.bind(this);
        this.changeFilePath = this.changeFilePath.bind(this);
        this.renderResultsTable = this.renderResultsTable.bind(this);
        this.setShowResults = this.setShowResults.bind(this);
        this.postFile = this.postFile.bind(this);
    }
    componentDidMount() {
        if (this.state.showResults) {
            this.getResults(undefined);
        }
        else {
            this.FetchFiles();
        }
    }

    componentDidUpdate(prevProps, prevState) {
        var str = prevProps.location.pathname;
        var n = str.lastIndexOf('/');
        var prev_location = str.substring(0, n + 1);
        var b = (this.props.location.pathname.toLowerCase() === "/ondemandfiles") && (prev_location.toLowerCase() === "/ondemandfiles/");
        if (b) {
            this.setState({ showResults: false });
        }
    }

    FormatToMMss(seconds) {
        var date = new Date(null);
        date.setSeconds(seconds);
        var str = date.toISOString().substr(14, 5);
        return str;
    }
    renderResultsTable(results) {
        return (
            <div> 
                <textarea value={JSON.stringify(this.state.fileDTO, undefined, 4)} rows="15" cols="100" />
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>reference</th>
                        <th>title</th>
                        <th>artists</th>
                        <th>startTime</th>
                        <th>endTime</th>
                        <th>duration</th>
                        <th>accuracy</th>
                    </tr>
                </thead>
                <tbody>
                    {results.map(r =>
                        <tr key={r.id}>
                            <td>{r.reference}</td>
                            <td>{r.title}</td>
                            <td>{r.artists}</td>
                            <td>{r.start_time}</td>
                            <td>{r.end_time}</td>
                            <td>{this.FormatToMMss(r.duration)}</td>
                            <td>{r.accuracy}</td>
                        </tr>
                    )}
                </tbody>
                </table>
                </div>
        );
    }

    changeFileID(event) {
        this.setState({ fileID: event.target.valueAsNumber });
    }

    changeFilePath(event) {
        this.setState({ filePath: event.target.value });
    }

    changeFilters(event) {
        this.setState({ filters: event.target.checked });
    }
    setShowResults(id) {
        this.setState({ showResults: true, fileID: id }, this.getResults);
        
    }

    renderFilesTable(files) {
        return (
            <div>
                
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>id</th>
                        <th>path</th>
                        <th>duration</th>
                        <th>jobCreated</th>
                        <th>jobUpdated</th>
                        <th>jobUser</th>
                        <th>jobPct</th>
                        <th>jobDuration</th>
                        <th>jobFinished</th>
                    </tr>
                </thead>
                <tbody>
                    {files.map(f =>
                        <tr key={f.id}>
                            <td><Link onClick={() => { this.setShowResults(f.id) }} to={{ pathname: ('/onDemandFiles/' + f.id), aboutProps: { file_id: f.id } }} >{f.id}</Link></td>
                            <td>{"...\\"+f.file_path.substring(f.file_path.lastIndexOf('\\') +1 )}</td>
                            <td>{f.file_duration}</td>
                            <td>{f.created}</td>
                            <td>{f.last_updated}</td>
                            <td>{f.user != null ? f.user : "--"}</td>
                            <td>{f.percentage}</td>
                            <td>{f.time_used}</td>
                            <td>{f.job_finished + ""}</td>
                        </tr>
                    )
                    }
                </tbody>
                </table>
            </div>
        );
    }

    render() {

        if (this.state.showResults) { // are we about to show results for a single file?
        let contents = this.state.loading_results
            ? <p><em>Loading...</em></p>
            : this.renderResultsTable(this.state.results);
            return (
                <div>
                    <h5 id="tabelLabel" >OnDemandFiles / {this.state.fileID}</h5>
                    
                    {contents}
                </div>
            );
        }
        else {
            let contents = this.state.loading_files
                ? <p><em>Loading...</em></p>
                : this.renderFilesTable(this.state.files);
            return (
                <div>
                    <h5 id="tabelLabel" >OnDemandFiles</h5>

                    <form onSubmit={this.postFile}>
                        <label>
                            Path:
                <input type="text" size="100" name="File Path" onChange={this.changeFilePath} />
                        </label>

                        <input type="submit" text="Post file for analysis" value="POST"/>
                    </form>

                    <br />
                    {contents}
                </div>
            );
        }
    }

    async getResults(event) {
        if (event != undefined) event.preventDefault();
        this.setState({ loading_results: true });
        const settings = {
            method: "get",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        };
        const url = "api/ondemandfiles/" + this.state.fileID + "?filters=" + this.state.filters;

        const response = await fetch(url, settings);
        const json = await response.json();

        this.setState({
            results: json.results,
            fileDTO: json.file,
            loading_results: false
        });
        //  alert("A file was submitted for analysis: " + data);
    }

    async FetchFiles() {
        this.setState({ loading_files: true });
        const response = await fetch('api/ondemandfiles');
        const data = await response.json();
        this.setState({ files: data, loading_files: false });
    }

    async postFile(e) {
        e.preventDefault();
        const input = { "audioPath": this.state.filePath, "user": this.state.user };
        const settings = {
            method: "post",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        };
        const url = "api/files/";

        const response = await fetch(url, settings);
        const data = await response.text();

        this.setState({ loading: false });
        this.FetchFiles();
        alert("A file was submitted for analysis: " + data);
    }

}
